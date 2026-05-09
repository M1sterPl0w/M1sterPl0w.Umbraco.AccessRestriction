using M1sterPl0w.Umbraco.AccessRestriction;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace M1sterPl0w.Umbraco.AccessRestrictionTests.Helpers;

/// <summary>Fluent builder that constructs a <see cref="DefaultHttpContext"/> for unit tests.</summary>
internal sealed class TestHttpContextBuilder
{
    private readonly DefaultHttpContext _context = new();

    /// <summary>Sets the raw socket remote IP (used when no IP header is configured).</summary>
    public TestHttpContextBuilder WithRemoteIp(string ip)
    {
        _context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return this;
    }

    /// <summary>
    /// Stores one or more resolved IPs in <c>HttpContext.Items</c>, matching what the middleware
    /// writes after calling <c>ExtractClientIps</c>.
    /// </summary>
    public TestHttpContextBuilder WithIpItems(params string[] ips)
    {
        _context.Items[Constants.ClientIpItemKey] = ips.ToList();
        return this;
    }

    /// <summary>Sets the request path.</summary>
    public TestHttpContextBuilder WithPath(string path)
    {
        _context.Request.Path = new PathString(path);
        return this;
    }

    /// <summary>Sets an authenticated user that belongs to the specified Umbraco role(s).</summary>
    public TestHttpContextBuilder WithAuthenticatedUser(params string[] roles)
    {
        var claims = roles.Select(r => new Claim(ClaimTypes.Role, r));
        // Passing an authenticationType makes IsAuthenticated = true
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        _context.User = new ClaimsPrincipal(identity);
        return this;
    }

    /// <summary>Explicitly sets an unauthenticated (anonymous) user.</summary>
    public TestHttpContextBuilder WithUnauthenticatedUser()
    {
        // ClaimsIdentity with no authenticationType → IsAuthenticated = false
        _context.User = new ClaimsPrincipal(new ClaimsIdentity());
        return this;
    }

    public HttpContext Build() => _context;
}
