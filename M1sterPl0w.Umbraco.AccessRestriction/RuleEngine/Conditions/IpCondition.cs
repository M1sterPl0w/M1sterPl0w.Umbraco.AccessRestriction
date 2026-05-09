using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    /// <summary>Matches when any of the resolved client IPs is in the configured list.</summary>
    public class IpCondition : IAccessCondition
    {
        public List<string> AllowedIps { get; set; } = [];

        public bool IsMatch(HttpContext context)
        {
            var ips = context.Items[Constants.ClientIpItemKey] as List<string>
                ?? [context.Connection.RemoteIpAddress?.ToString() ?? ""];

            return ips.Any(ip => AllowedIps.Contains(ip, StringComparer.OrdinalIgnoreCase));
        }
    }
}
