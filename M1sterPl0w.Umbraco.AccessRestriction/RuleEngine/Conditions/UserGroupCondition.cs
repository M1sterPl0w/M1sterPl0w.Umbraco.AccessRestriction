using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    public class UserGroupCondition : IAccessCondition
    {
        public List<string> Groups { get; set; } = [];

        public bool IsMatch(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
               return false;
            }

            return Groups.Any(group => context.User.IsInRole(group));
        }
    }
}
