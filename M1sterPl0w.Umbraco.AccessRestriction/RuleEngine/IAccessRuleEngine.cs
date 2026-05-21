using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    public interface IAccessRuleEngine
    {
        Task<bool> EvaluateAsync(HttpContext context);
    }
}
