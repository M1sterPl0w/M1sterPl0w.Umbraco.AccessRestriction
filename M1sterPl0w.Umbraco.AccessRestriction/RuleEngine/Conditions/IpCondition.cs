using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    /// <summary>
    /// Matches when any of the resolved client IPs is in the configured list.
    /// Each entry in <see cref="AllowedIps"/> can be:
    /// <list type="bullet">
    ///   <item>An exact IP address (e.g. <c>192.168.1.1</c>)</item>
    ///   <item>A CIDR range (e.g. <c>192.168.1.0/24</c>)</item>
    ///   <item>A wildcard pattern using <c>*</c> or <c>?</c> (e.g. <c>192.168.1.*</c>)</item>
    /// </list>
    /// </summary>
    public class IpCondition : IAccessCondition
    {
        public List<string> AllowedIps { get; set; } = [];

        public bool IsMatch(HttpContext context)
        {
            var ips = context.Items[Constants.ClientIpItemKey] as List<string>
                ?? [context.Connection.RemoteIpAddress?.ToString() ?? ""];

            return ips.Any(ip => AllowedIps.Any(pattern => IpMatchesPattern(ip, pattern)));
        }

        private static bool IpMatchesPattern(string ip, string pattern)
        {
            // 1. Exact match
            if (string.Equals(ip, pattern, StringComparison.OrdinalIgnoreCase))
                return true;

            // 2. CIDR range (e.g. 192.168.1.0/24 or 2001:db8::/32)
            if (pattern.Contains('/') &&
                IPAddress.TryParse(ip, out var ipAddr) &&
                IPNetwork.TryParse(pattern, out var network))
            {
                return network.Contains(ipAddr);
            }

            // 3. Wildcard pattern using * or ? (e.g. 192.168.1.* or 10.0.?.1)
            if (pattern.Contains('*') || pattern.Contains('?'))
            {
                var regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";
                return Regex.IsMatch(ip, regexPattern, RegexOptions.IgnoreCase);
            }

            return false;
        }
    }
}
