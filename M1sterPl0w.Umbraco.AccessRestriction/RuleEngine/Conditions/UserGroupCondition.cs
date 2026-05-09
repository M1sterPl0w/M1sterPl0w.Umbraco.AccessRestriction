using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    /// <summary>
    /// Matches when the authenticated user belongs to any of the configured Umbraco user groups.
    /// Requires the authentication middleware to have run before this middleware.
    /// </summary>
    public class UserGroupCondition : IAccessCondition
    {
        public List<string> Groups { get; set; } = [];

        public bool IsMatch(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return false;

            return Groups.Any(group => context.User.IsInRole(group));
        }
    }
}
