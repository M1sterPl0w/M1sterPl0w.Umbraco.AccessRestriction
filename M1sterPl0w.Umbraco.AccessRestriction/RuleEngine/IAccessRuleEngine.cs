using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    public interface IAccessRuleEngine
    {
        /// <summary>
        /// Evaluates all configured access rules against the current request.
        /// Returns <c>true</c> if the request should be allowed; <c>false</c> to deny with 403.
        /// </summary>
        Task<bool> EvaluateAsync(HttpContext context);
    }
}
