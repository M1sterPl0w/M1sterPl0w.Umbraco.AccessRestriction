using M1sterPl0w.Umbraco.AccessRestriction.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.Scoping;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public class IpAddressRepository : IIpAddressRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMemoryCache _cache;
        private readonly IOptions<AccessRestrictionOptions> _options;
        private static readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

        public IpAddressRepository(IScopeProvider scopeProvider, IMemoryCache cache, IOptions<AccessRestrictionOptions> options)
        {
            _scopeProvider = scopeProvider;
            _cache = cache;
            _options = options;
        }

        public async Task<IReadOnlyList<AllowedIpAddressDto>> GetAllAsync()
        {
            if (!_cache.TryGetValue(Constants.CacheKeys.IpList, out IReadOnlyList<AllowedIpAddressDto>? cached) || cached is null)
            {
                cached = await FetchFromDbAsync();
                _cache.Set(Constants.CacheKeys.IpList, cached, _cacheOptions);
            }
            return cached;
        }

        public async Task<bool> AddAsync(string ipAddress, string? description, string? modifiedBy)
        {
            using var scope = _scopeProvider.CreateScope();
            try
            {
                var createdDate = DateTime.UtcNow;
                await scope.Database.InsertAsync(new AllowedIpAddressSchema
                {
                    IpAddress = ipAddress,
                    Description = description,
                    CreatedDate = createdDate,
                    CreatedBy = modifiedBy
                });
                scope.Complete();

                // Write-through: get current cached list (or fetch from DB if cold), then append
                var current = _cache.TryGetValue(Constants.CacheKeys.IpList, out IReadOnlyList<AllowedIpAddressDto>? existing) && existing is not null
                    ? existing
                    : await FetchFromDbAsync();
                _cache.Set<IReadOnlyList<AllowedIpAddressDto>>(Constants.CacheKeys.IpList,
                    current.Append(new AllowedIpAddressDto { IpAddress = ipAddress, Description = description, CreatedDate = createdDate, CreatedBy = modifiedBy }).ToList(),
                    _cacheOptions);
                return true;
            }
            catch
            {
                // Primary key violation — IP already exists
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string ipAddress)
        {
            using var scope = _scopeProvider.CreateScope();
            var affected = scope.Database.Execute(
                $"DELETE FROM {AllowedIpAddressSchema.TableName} WHERE IpAddress = @0",
                ipAddress);
            scope.Complete();

            if (affected > 0)
            {
                // Write-through: get current cached list (or fetch from DB if cold), then remove
                var current = _cache.TryGetValue(Constants.CacheKeys.IpList, out IReadOnlyList<AllowedIpAddressDto>? existing) && existing is not null
                    ? existing
                    : await FetchFromDbAsync();
                _cache.Set<IReadOnlyList<AllowedIpAddressDto>>(Constants.CacheKeys.IpList,
                    current.Where(e => !string.Equals(e.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase)).ToList(),
                    _cacheOptions);
            }
            return affected > 0;
        }

        public async Task<bool> IsAllowedAsync(string ipAddress)
        {
            var all = await GetAllAsync();
            return all.Any(e => string.Equals(e.IpAddress, ipAddress, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<IReadOnlyList<AllowedIpAddressDto>> FetchFromDbAsync()
        {
            using var scope = _scopeProvider.CreateScope();
            var rows = await scope.Database.FetchAsync<AllowedIpAddressSchema>();
            scope.Complete();

            var staticIps = _options.Value.IpAddresses
                .Select(e => new AllowedIpAddressDto
                {
                    IpAddress = e.IpAddress,
                    Description = e.Description,
                    CanDelete = false
                });

            return staticIps
                .Concat(rows
                    .Where(r => !_options.Value.IpAddresses.Any(s =>
                        string.Equals(s.IpAddress, r.IpAddress, StringComparison.OrdinalIgnoreCase)))
                    .Select(r => new AllowedIpAddressDto
                    {
                        IpAddress = r.IpAddress,
                        Description = r.Description,
                        CreatedDate = r.CreatedDate,
                        CreatedBy = r.CreatedBy,
                        CanDelete = true
                    }))
                .ToList();
        }
    }
}
