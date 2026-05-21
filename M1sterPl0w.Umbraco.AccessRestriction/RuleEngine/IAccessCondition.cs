using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    public interface IAccessCondition
    {
        bool IsMatch(HttpContext context);
    }
}
