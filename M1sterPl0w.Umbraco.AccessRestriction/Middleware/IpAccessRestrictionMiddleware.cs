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

        public async Task InvokeAsync(HttpContext context, IAccessRuleEngine ruleEngine, ISettingsRepository settingsRepository, IContentUrlResolver contentUrlResolver)
        {
            var settings =  await settingsRepository.GetAsync();
            if (!settings.Enabled)
            {
                await _next(context);
                return;
            }

            context.Items[Constants.ClientIpItemKey] = ExtractClientIps(context, settings.IpHeader, settings.ConsiderRemoteIp);

            if (!await ruleEngine.EvaluateAsync(context))
            {
                if (settings.DenyContentNodeKey.HasValue)
                {
                    var redirectUrl = contentUrlResolver.GetUrl(settings.DenyContentNodeKey.Value);
                    if (!string.IsNullOrEmpty(redirectUrl))
                    {
                        var requestPath = context.Request.Path.Value ?? string.Empty;
                        var redirectPath = Uri.TryCreate(redirectUrl, UriKind.Absolute, out var uri)
                            ? uri.AbsolutePath
                            : redirectUrl.Split('?')[0];

                        if (string.Equals(requestPath.TrimEnd('/'), redirectPath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                        {
                            await _next(context);
                            return;
                        }

                        context.Response.Redirect(redirectUrl);
                        return;
                    }
                }

                context.Response.StatusCode = settings.DenyStatusCode;
                await context.Response.WriteAsync("Access denied.");
                return;
            }

            await _next(context);
        }

        private static List<string> ExtractClientIps(HttpContext context, string? ipHeader, bool considerRemoteIp)
        {
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp?.IsIPv4MappedToIPv6 == true)
            {
                remoteIp = remoteIp.MapToIPv4();
            }

            var remoteIpStr = remoteIp?.ToString();

            if (!string.IsNullOrWhiteSpace(ipHeader))
            {
                var headerValue = context.Request.Headers[ipHeader].FirstOrDefault();
                var headerIp = headerValue?.Split(',')[0].Trim();

                var ips = new List<string>();
                if (headerIp != null)
                {
                    ips.Add(headerIp);
                }

                if (considerRemoteIp && remoteIpStr != null)
                {
                    ips.Add(remoteIpStr);
                }
                
                return ips;
            }

            return remoteIpStr != null ? [remoteIpStr] : [];
        }
    }
}

