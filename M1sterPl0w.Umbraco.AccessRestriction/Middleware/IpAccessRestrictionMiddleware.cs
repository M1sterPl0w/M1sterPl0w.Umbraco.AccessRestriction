using M1sterPl0w.Umbraco.AccessRestriction.Services;
using Microsoft.AspNetCore.Http;
namespace M1sterPl0w.Umbraco.AccessRestriction.Middleware
{
    public class IpAccessRestrictionMiddleware
    {
        private readonly RequestDelegate _next;

        public IpAccessRestrictionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IIpAddressRepository repository, ISettingsRepository settingsRepository, IRestrictedPathRepository pathsRepository)
        {
            var settings = await settingsRepository.GetAsync();

            // If restriction is disabled and not forced by appsettings, allow everything
            if (!settings.Enabled && !settings.IsEnabledForced)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;
            var restrictedPaths = await pathsRepository.GetAllAsync();

            // If the request path does not match any configured restricted path, allow it
            if (restrictedPaths.Count > 0 && !restrictedPaths.Any(rp =>
            {
                var rpNorm = rp.Path.TrimEnd('/');
                return path.Equals(rpNorm, StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith(rpNorm + "/", StringComparison.OrdinalIgnoreCase);
            }))
            {
                await _next(context);
                return;
            }

            var allowedIps = await repository.GetAllAsync();

            // If no IPs are configured, the restriction is inactive — allow everything
            if (allowedIps.Count == 0)
            {
                await _next(context);
                return;
            }

            var clientIp = context.Connection.RemoteIpAddress;
            if (clientIp is not null)
            {
                // Normalise IPv4-mapped IPv6 addresses (::ffff:x.x.x.x → x.x.x.x)
                if (clientIp.IsIPv4MappedToIPv6)
                    clientIp = clientIp.MapToIPv4();

                if (allowedIps.Any(e => e.IpAddress == clientIp.ToString()))
                {
                    await _next(context);
                    return;
                }
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: your IP address is not allowed.");
        }
    }
}
