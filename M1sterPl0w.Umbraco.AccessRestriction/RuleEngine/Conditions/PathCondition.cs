using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    /// <summary>Matches when the request path starts with any of the configured paths.</summary>
    public class PathCondition : IAccessCondition
    {
        public List<string> Paths { get; set; } = [];

        public bool IsMatch(HttpContext context)
        {
            var requestPath = context.Request.Path.Value ?? string.Empty;

            return Paths.Any(p =>
            {
                var normalized = p.TrimEnd('/');
                return requestPath.Equals(normalized, StringComparison.OrdinalIgnoreCase)
                    || requestPath.StartsWith(normalized + "/", StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}
