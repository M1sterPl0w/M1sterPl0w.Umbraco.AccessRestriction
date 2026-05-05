using M1sterPl0w.Umbraco.AccessRestriction.Models;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public interface IIpAddressRepository
    {
        Task<IReadOnlyList<AllowedIpAddressDto>> GetAllAsync();
        Task<bool> AddAsync(string ipAddress, string? description, string? modifiedBy);
        Task<bool> DeleteAsync(string ipAddress);
        Task<bool> IsAllowedAsync(string ipAddress);
    }
}
