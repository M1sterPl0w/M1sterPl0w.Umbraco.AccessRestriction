using M1sterPl0w.Umbraco.AccessRestriction.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.Scoping;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMemoryCache _cache;
        private readonly IOptions<AccessRestrictionOptions> _options;
        private static readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

        public SettingsRepository(IScopeProvider scopeProvider, IMemoryCache cache, IOptions<AccessRestrictionOptions> options)
        {
            _scopeProvider = scopeProvider;
            _cache = cache;
            _options = options;
        }

        public async Task<SettingsDto> GetAsync()
        {
            if (!_cache.TryGetValue(Constants.CacheKeys.Settings, out SettingsDto? cached) || cached is null)
            {
                cached = await FetchFromDbAsync();
                _cache.Set(Constants.CacheKeys.Settings, cached, _cacheOptions);
            }
            return cached;
        }

        public Task SaveAsync(SettingsDto settings)
        {
            using var scope = _scopeProvider.CreateScope();

            Upsert(scope, AccessRestrictionSettingsSchema.KeyEnabled, settings.Enabled ? "true" : "false");

            scope.Complete();
            // Write-through: always recompute IsEnabledForced from options
            var toCache = new SettingsDto
            {
                Enabled = settings.Enabled,
                IsEnabledForced = _options.Value.IpAddresses.Count > 0
            };
            _cache.Set(Constants.CacheKeys.Settings, toCache, _cacheOptions);
            return Task.CompletedTask;
        }

        private static void Upsert(IScope scope, string key, string value)
        {
            var exists = scope.Database.ExecuteScalar<int>(
                $"SELECT COUNT(1) FROM \"{AccessRestrictionSettingsSchema.TableName}\" WHERE \"Key\" = @0", key) > 0;

            if (exists)
                scope.Database.Execute(
                    $"UPDATE \"{AccessRestrictionSettingsSchema.TableName}\" SET \"Value\" = @0 WHERE \"Key\" = @1", value, key);
            else
                scope.Database.Execute(
                    $"INSERT INTO \"{AccessRestrictionSettingsSchema.TableName}\" (\"Key\", \"Value\") VALUES (@0, @1)", key, value);
        }

        private async Task<SettingsDto> FetchFromDbAsync()
        {
            using var scope = _scopeProvider.CreateScope();
            var rows = await scope.Database.FetchAsync<AccessRestrictionSettingsSchema>();
            scope.Complete();
            var lookup = rows.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);
            return new SettingsDto
            {
                Enabled = ParseBool(lookup, AccessRestrictionSettingsSchema.KeyEnabled, defaultValue: true),
                IsEnabledForced = _options.Value.IpAddresses.Count > 0
            };
        }

        private static bool ParseBool(Dictionary<string, string?> lookup, string key, bool defaultValue)
        {
            if (lookup.TryGetValue(key, out var val) && bool.TryParse(val, out var result))
                return result;
            return defaultValue;
        }
    }
}
