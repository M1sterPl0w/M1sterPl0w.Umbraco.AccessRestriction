using M1sterPl0w.Umbraco.AccessRestriction.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Infrastructure.Scoping;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public class RestrictedPathRepository : IRestrictedPathRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IMemoryCache _cache;
        private readonly IOptions<AccessRestrictionOptions> _options;
        private static readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));

        public RestrictedPathRepository(IScopeProvider scopeProvider, IMemoryCache cache, IOptions<AccessRestrictionOptions> options)
        {
            _scopeProvider = scopeProvider;
            _cache = cache;
            _options = options;
        }

        public async Task<IReadOnlyList<RestrictedPathDto>> GetAllAsync()
        {
            if (!_cache.TryGetValue(Constants.CacheKeys.RestrictedPaths, out IReadOnlyList<RestrictedPathDto>? cached) || cached is null)
            {
                cached = await FetchFromDbAsync();
                _cache.Set(Constants.CacheKeys.RestrictedPaths, cached, _cacheOptions);
            }
            return cached;
        }

        public async Task<bool> AddAsync(string path, string? description, string? createdBy)
        {
            using var scope = _scopeProvider.CreateScope();
            try
            {
                var createdDate = DateTime.UtcNow;
                await scope.Database.InsertAsync(new RestrictedPathSchema
                {
                    Path = path,
                    Description = description,
                    CreatedDate = createdDate,
                    CreatedBy = createdBy
                });
                scope.Complete();

                var current = _cache.TryGetValue(Constants.CacheKeys.RestrictedPaths, out IReadOnlyList<RestrictedPathDto>? existing) && existing is not null
                    ? existing
                    : await FetchFromDbAsync();
                _cache.Set<IReadOnlyList<RestrictedPathDto>>(Constants.CacheKeys.RestrictedPaths,
                    current.Append(new RestrictedPathDto { Path = path, Description = description, CreatedDate = createdDate, CreatedBy = createdBy }).ToList(),
                    _cacheOptions);
                return true;
            }
            catch
            {
                // Primary key violation — path already exists
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string path)
        {
            using var scope = _scopeProvider.CreateScope();
            var affected = scope.Database.Execute(
                $"DELETE FROM \"{RestrictedPathSchema.TableName}\" WHERE \"Path\" = @0",
                path);
            scope.Complete();

            if (affected > 0)
            {
                var current = _cache.TryGetValue(Constants.CacheKeys.RestrictedPaths, out IReadOnlyList<RestrictedPathDto>? existing) && existing is not null
                    ? existing
                    : await FetchFromDbAsync();
                _cache.Set<IReadOnlyList<RestrictedPathDto>>(Constants.CacheKeys.RestrictedPaths,
                    current.Where(e => !string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase)).ToList(),
                    _cacheOptions);
            }
            return affected > 0;
        }

        private async Task<IReadOnlyList<RestrictedPathDto>> FetchFromDbAsync()
        {
            using var scope = _scopeProvider.CreateScope();
            var rows = await scope.Database.FetchAsync<RestrictedPathSchema>();
            scope.Complete();

            var staticPaths = _options.Value.Paths
                .Select(e => new RestrictedPathDto
                {
                    Path = e.Path.TrimEnd('/'),
                    Description = e.Description,
                    CanDelete = false
                });

            return staticPaths
                .Concat(rows
                    .Where(r => !_options.Value.Paths.Any(s =>
                        string.Equals(s.Path.TrimEnd('/'), r.Path, StringComparison.OrdinalIgnoreCase)))
                    .Select(r => new RestrictedPathDto
                    {
                        Path = r.Path,
                        Description = r.Description,
                        CreatedDate = r.CreatedDate,
                        CreatedBy = r.CreatedBy,
                        CanDelete = true
                    }))
                .ToList();
        }
    }
}
