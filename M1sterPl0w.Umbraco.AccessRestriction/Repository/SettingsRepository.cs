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

            // Only persist IpHeader when it is not forced by appsettings
            if (string.IsNullOrWhiteSpace(_options.Value.IpHeader))
                Upsert(scope, AccessRestrictionSettingsSchema.KeyIpHeader, settings.IpHeader ?? string.Empty);

            Upsert(scope, AccessRestrictionSettingsSchema.KeyConsiderRemoteIp, settings.ConsiderRemoteIp ? "true" : "false");

            scope.Complete();

            var ipHeaderForced = !string.IsNullOrWhiteSpace(_options.Value.IpHeader);
            _cache.Set(Constants.CacheKeys.Settings, new SettingsDto
            {
                Enabled          = settings.Enabled,
                IpHeader         = ipHeaderForced ? _options.Value.IpHeader : settings.IpHeader,
                IsIpHeaderForced = ipHeaderForced,
                ConsiderRemoteIp = settings.ConsiderRemoteIp
            }, _cacheOptions);
            return Task.CompletedTask;
        }

        private static void Upsert(IScope scope, string key, string value)
        {
            var entity = new AccessRestrictionSettingsSchema { Key = key, Value = value };
            // Fetch all rows (max ~3) and check in memory — avoids raw SQL quoting issues across databases
            var exists = scope.Database.Fetch<AccessRestrictionSettingsSchema>()
                .Any(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));
            if (exists)
                scope.Database.Update(entity);
            else
                scope.Database.Insert(entity);
        }

        private async Task<SettingsDto> FetchFromDbAsync()
        {
            using var scope = _scopeProvider.CreateScope();
            var rows = await scope.Database.FetchAsync<AccessRestrictionSettingsSchema>();
            scope.Complete();
            var lookup = rows.ToDictionary(r => r.Key, r => r.Value, StringComparer.OrdinalIgnoreCase);

            var ipHeaderForced = !string.IsNullOrWhiteSpace(_options.Value.IpHeader);
            var ipHeader = ipHeaderForced
                ? _options.Value.IpHeader
                : ParseString(lookup, AccessRestrictionSettingsSchema.KeyIpHeader);

            return new SettingsDto
            {
                Enabled          = ParseBool(lookup, AccessRestrictionSettingsSchema.KeyEnabled, defaultValue: true),
                IpHeader         = ipHeader,
                IsIpHeaderForced = ipHeaderForced,
                ConsiderRemoteIp = ParseBool(lookup, AccessRestrictionSettingsSchema.KeyConsiderRemoteIp, defaultValue: false)
            };
        }

        private static string? ParseString(Dictionary<string, string?> lookup, string key)
        {
            if (lookup.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val;
            return null;
        }

        private static bool ParseBool(Dictionary<string, string?> lookup, string key, bool defaultValue)
        {
            if (lookup.TryGetValue(key, out var val) && bool.TryParse(val, out var result))
                return result;
            return defaultValue;
        }
    }
}

