using M1sterPl0w.Umbraco.AccessRestriction.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Umbraco.Cms.Infrastructure.Scoping;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public class RuleRepository : IRuleRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMemoryCache _cache;
        private readonly IOptions<AccessRestrictionOptions> _options;

        private static readonly MemoryCacheEntryOptions CacheOptions =
            new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));

        public RuleRepository(IScopeProvider scopeProvider, IMemoryCache cache, IOptions<AccessRestrictionOptions> options)
        {
            _scopeProvider = scopeProvider;
            _cache = cache;
            _options = options;
        }

        public async Task<IReadOnlyList<AccessRuleDto>> GetAllAsync()
        {
            if (!_cache.TryGetValue(Constants.CacheKeys.Rules, out IReadOnlyList<AccessRuleDto>? cached) || cached is null)
            {
                cached = await FetchFromDbAsync();
                _cache.Set(Constants.CacheKeys.Rules, cached, CacheOptions);
            }
            return cached;
        }

        public async Task<int> CreateRuleAsync(CreateRuleRequest request, string? createdBy)
        {
            using var scope = _scopeProvider.CreateScope();
            var schema = new AccessRuleSchema
            {
                Name       = request.Name,
                Description = request.Description,
                RequireAll  = request.RequireAll,
                Result      = request.Result,
                SortOrder   = 0,
                CreatedDate = DateTime.UtcNow,
                CreatedBy   = createdBy
            };
            var newId = Convert.ToInt32(await scope.Database.InsertAsync(schema));
            scope.Complete();
            InvalidateCache();
            return newId;
        }

        public async Task<bool> UpdateRuleAsync(int id, UpdateRuleRequest request)
        {
            using var scope = _scopeProvider.CreateScope();
            // Fetch the existing row by PK — WHERE fragment lets NPoco handle table/column quoting
            var existing = scope.Database.Fetch<AccessRuleSchema>("WHERE Id = @0", id).FirstOrDefault();
            if (existing is null) { scope.Complete(); return false; }
            existing.Name        = request.Name;
            existing.Description = request.Description;
            existing.RequireAll  = request.RequireAll;
            existing.Result      = request.Result;
            existing.SortOrder   = request.SortOrder;
            scope.Database.Update(existing);
            scope.Complete();
            InvalidateCache();
            return true;
        }

        public async Task<bool> DeleteRuleAsync(int id)
        {
            using var scope = _scopeProvider.CreateScope();
            var rule = scope.Database.Fetch<AccessRuleSchema>("WHERE Id = @0", id).FirstOrDefault();
            if (rule is null) { scope.Complete(); return false; }
            // Remove conditions belonging to this rule first
            var conditions = scope.Database.Fetch<AccessConditionSchema>("WHERE RuleId = @0", id);
            foreach (var condition in conditions)
                scope.Database.Delete(condition);
            scope.Database.Delete(rule);
            scope.Complete();
            InvalidateCache();
            return true;
        }

        public async Task<int> AddConditionAsync(int ruleId, CreateConditionRequest request)
        {
            var valuesJson = JsonSerializer.Serialize(request.Values);
            using var scope = _scopeProvider.CreateScope();
            var schema = new AccessConditionSchema
            {
                RuleId        = ruleId,
                ConditionType = request.Type,
                Values        = valuesJson
            };
            var newId = Convert.ToInt32(await scope.Database.InsertAsync(schema));
            scope.Complete();
            InvalidateCache();
            return newId;
        }

        public async Task<bool> DeleteConditionAsync(int conditionId)
        {
            using var scope = _scopeProvider.CreateScope();
            var condition = scope.Database.Fetch<AccessConditionSchema>("WHERE Id = @0", conditionId).FirstOrDefault();
            if (condition is null) { scope.Complete(); return false; }
            scope.Database.Delete(condition);
            scope.Complete();
            InvalidateCache();
            return true;
        }

        private void InvalidateCache() => _cache.Remove(Constants.CacheKeys.Rules);

        private async Task<IReadOnlyList<AccessRuleDto>> FetchFromDbAsync()
        {
            using var scope = _scopeProvider.CreateScope();
            var rules      = await scope.Database.FetchAsync<AccessRuleSchema>();
            var conditions = await scope.Database.FetchAsync<AccessConditionSchema>();
            scope.Complete();

            var conditionsByRule = conditions
                .GroupBy(c => c.RuleId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var dbRules = rules
                .OrderBy(r => r.SortOrder)
                .Select(r => new AccessRuleDto
                {
                    Id          = r.Id,
                    Name        = r.Name,
                    Description = r.Description,
                    RequireAll  = r.RequireAll,
                    Result      = r.Result,
                    SortOrder   = r.SortOrder,
                    CanDelete   = true,
                    CreatedBy   = r.CreatedBy,
                    CreatedDate = r.CreatedDate,
                    Conditions  = conditionsByRule.TryGetValue(r.Id, out var conds)
                        ? conds.Select(c => new ConditionDto
                        {
                            Id       = c.Id,
                            Type     = c.ConditionType,
                            Values   = DeserializeValues(c.Values),
                            CanDelete = true
                        }).ToList()
                        : []
                })
                .ToList();

            // Static rules from appsettings are prepended with negative IDs and CanDelete = false
            var staticRules = _options.Value.Rules
                .Select((r, i) => new AccessRuleDto
                {
                    Id          = -(i + 1),
                    Name        = r.Name,
                    Description = r.Description,
                    RequireAll  = r.RequireAll,
                    Result      = r.Result,
                    SortOrder   = r.SortOrder,
                    CanDelete   = false,
                    Conditions  = r.Conditions.Select((c, ci) => new ConditionDto
                    {
                        Id        = -(ci + 1),
                        Type      = c.Type,
                        Values    = c.Values,
                        CanDelete = false
                    }).ToList()
                })
                .ToList();

            return [.. staticRules, .. dbRules];
        }

        private static List<string> DeserializeValues(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
            catch  { return []; }
        }
    }
}
