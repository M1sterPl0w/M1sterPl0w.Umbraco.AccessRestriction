using M1sterPl0w.Umbraco.AccessRestriction.Models;

namespace M1sterPl0w.Umbraco.AccessRestriction.Services
{
    public interface IRuleRepository
    {
        Task<IReadOnlyList<AccessRuleDto>> GetAllAsync();

        Task<int> CreateRuleAsync(CreateRuleRequest request, string? createdBy);
        
        Task<bool> UpdateRuleAsync(int id, UpdateRuleRequest request);
        
        Task<bool> DeleteRuleAsync(int id);
        
        Task<int> AddConditionAsync(int ruleId, CreateConditionRequest request);
        
        Task<bool> DeleteConditionAsync(int conditionId);
    }
}
