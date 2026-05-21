using M1sterPl0w.Umbraco.AccessRestriction.Models;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public interface ISettingsRepository
    {
        Task<SettingsDto> GetAsync();
        
        Task SaveAsync(SettingsDto settings);
    }
}
