using M1sterPl0w.Umbraco.AccessRestriction.Models;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public interface IRestrictedPathRepository
    {
        Task<IReadOnlyList<RestrictedPathDto>> GetAllAsync();
        Task<bool> AddAsync(string path, string? description, string? createdBy);
        Task<bool> DeleteAsync(string path);
    }
}
