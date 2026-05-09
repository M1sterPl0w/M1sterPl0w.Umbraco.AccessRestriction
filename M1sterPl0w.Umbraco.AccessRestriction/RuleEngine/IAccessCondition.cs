using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    /// <summary>Represents a single condition within an access rule.</summary>
    public interface IAccessCondition
    {
        bool IsMatch(HttpContext context);
    }
}
