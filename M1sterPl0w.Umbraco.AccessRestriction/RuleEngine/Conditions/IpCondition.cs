using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
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
            if (string.Equals(ip, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (pattern.Contains('/') &&
                IPAddress.TryParse(ip, out var ipAddr) &&
                IPNetwork.TryParse(pattern, out var network))
            {
                return network.Contains(ipAddr);
            }

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
