using M1sterPl0w.Umbraco.AccessRestriction.Models;
using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestriction.Services;
using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestriction.Middleware
{
    public class AccessRestrictionMiddleware
    {
        private readonly RequestDelegate _next;

        public AccessRestrictionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAccessRuleEngine ruleEngine, ISettingsRepository settingsRepository)
        {
            SettingsDto settings;
            try
            {
                settings = await settingsRepository.GetAsync();
            }
            catch
            {
                // Database tables are not yet available (e.g. first startup before migrations run).
                // Allow the request through so Umbraco can initialise and execute migrations.
                await _next(context);
                return;
            }

            if (!settings.Enabled)
            {
                await _next(context);
                return;
            }

            // Resolve the client IP(s) once and store them so conditions can read without re-fetching settings
            context.Items[Constants.ClientIpItemKey] = ExtractClientIps(context, settings.IpHeader, settings.ConsiderRemoteIp);

            if (!await ruleEngine.EvaluateAsync(context))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Access denied.");
                return;
            }

            await _next(context);
        }

        private static List<string> ExtractClientIps(HttpContext context, string? ipHeader, bool considerRemoteIp)
        {
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp?.IsIPv4MappedToIPv6 == true)
                remoteIp = remoteIp.MapToIPv4();
            var remoteIpStr = remoteIp?.ToString();

            if (!string.IsNullOrWhiteSpace(ipHeader))
            {
                var headerValue = context.Request.Headers[ipHeader].FirstOrDefault();
                var headerIp = headerValue?.Split(',')[0].Trim();

                var ips = new List<string>();
                if (headerIp != null) ips.Add(headerIp);
                if (considerRemoteIp && remoteIpStr != null) ips.Add(remoteIpStr);
                return ips;
            }

            return remoteIpStr != null ? [remoteIpStr] : [];
        }
    }
}

