using M1sterPl0w.Umbraco.AccessRestriction.Middleware;
using M1sterPl0w.Umbraco.AccessRestriction.Models;
using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestriction.Services;
using Microsoft.AspNetCore.Http;

namespace M1sterPl0w.Umbraco.AccessRestrictionTests.Middleware;

public class AccessRestrictionMiddlewareTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AccessRestrictionMiddleware CreateMiddleware(RequestDelegate? next = null)
        => new(next ?? (_ => Task.CompletedTask));

    private static Mock<ISettingsRepository> EnabledSettings(
        int denyStatusCode = 403,
        Guid? denyContentNodeKey = null)
    {
        var mock = new Mock<ISettingsRepository>();
        mock.Setup(r => r.GetAsync()).ReturnsAsync(new SettingsDto
        {
            Enabled            = true,
            IpHeader           = null,
            IsIpHeaderForced   = false,
            ConsiderRemoteIp   = false,
            DenyStatusCode     = denyStatusCode,
            DenyContentNodeKey = denyContentNodeKey
        });
        return mock;
    }

    private static Mock<IAccessRuleEngine> RuleEngine(bool allows)
    {
        var mock = new Mock<IAccessRuleEngine>();
        mock.Setup(e => e.EvaluateAsync(It.IsAny<HttpContext>())).ReturnsAsync(allows);
        return mock;
    }

    private static Mock<IContentUrlResolver> ContentUrlResolver(Guid? key = null, string? url = null)
    {
        var mock = new Mock<IContentUrlResolver>();
        if (key.HasValue)
            mock.Setup(r => r.GetUrl(key.Value)).Returns(url);
        return mock;
    }

    private static DefaultHttpContext BuildContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        return ctx;
    }

    // ── Status code ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_AccessDenied_Returns403ByDefault()
    {
        var middleware = CreateMiddleware();
        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings(denyStatusCode: 403).Object,
            ContentUrlResolver().Object);

        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AccessDenied_ReturnsCustomStatusCode()
    {
        var middleware = CreateMiddleware();
        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings(denyStatusCode: 404).Object,
            ContentUrlResolver().Object);

        Assert.Equal(404, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AccessDenied_StatusCode451_IsHonoured()
    {
        var middleware = CreateMiddleware();
        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings(denyStatusCode: 451).Object,
            ContentUrlResolver().Object);

        Assert.Equal(451, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AccessAllowed_DoesNotAlterStatusCode()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: true).Object,
            EnabledSettings().Object,
            ContentUrlResolver().Object);

        Assert.True(nextCalled);
        Assert.Equal(200, ctx.Response.StatusCode);
    }

    // ── Content node redirect ────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_AccessDenied_WithContentNodeKey_RedirectsToResolvedUrl()
    {
        var nodeKey = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var ctx = BuildContext();
        ctx.Request.Path = "/some-other-page";

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings(denyContentNodeKey: nodeKey).Object,
            ContentUrlResolver(nodeKey, "https://example.com/access-denied").Object);

        Assert.Equal(302, ctx.Response.StatusCode);
        Assert.Equal("https://example.com/access-denied", ctx.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_AccessDenied_AlreadyOnDenyPage_AbsoluteUrl_CallsNext()
    {
        // When already on the deny page, Umbraco should render the page rather than returning plain text.
        var nodeKey = Guid.NewGuid();
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = BuildContext();
        ctx.Request.Path = "/access-denied";

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings(denyStatusCode: 403, denyContentNodeKey: nodeKey).Object,
            ContentUrlResolver(nodeKey, "https://example.com/access-denied").Object);

        Assert.True(nextCalled);
        Assert.False(ctx.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_AccessDenied_AlreadyOnDenyPage_TrailingSlashIgnored()
    {
        var nodeKey = Guid.NewGuid();
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = BuildContext();
        ctx.Request.Path = "/access-denied/";

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings(denyStatusCode: 403, denyContentNodeKey: nodeKey).Object,
            ContentUrlResolver(nodeKey, "https://example.com/access-denied").Object);

        Assert.True(nextCalled);
        Assert.False(ctx.Response.Headers.ContainsKey("Location"));
    }

    [Fact]
    public async Task InvokeAsync_AccessDenied_ContentNodeKeySet_ButUrlUnresolvable_FallsBackToStatusCode()
    {
        var nodeKey = Guid.NewGuid();
        var middleware = CreateMiddleware();
        var ctx = BuildContext();

        // Resolver returns null (content not found / Umbraco not ready)
        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings(denyStatusCode: 403, denyContentNodeKey: nodeKey).Object,
            ContentUrlResolver(nodeKey, url: null).Object);

        Assert.Equal(403, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AccessDenied_NoContentNodeKey_DoesNotCallUrlResolver()
    {
        var resolverMock = new Mock<IContentUrlResolver>();
        var middleware = CreateMiddleware();
        var ctx = BuildContext();

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            EnabledSettings().Object,
            resolverMock.Object);

        resolverMock.Verify(r => r.GetUrl(It.IsAny<Guid>()), Times.Never);
        Assert.Equal(403, ctx.Response.StatusCode);
    }

    // ── Disabled middleware ───────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_Disabled_AlwaysCallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = BuildContext();

        var settingsMock = new Mock<ISettingsRepository>();
        settingsMock.Setup(r => r.GetAsync()).ReturnsAsync(new SettingsDto
        {
            Enabled = false, IpHeader = null, IsIpHeaderForced = false, ConsiderRemoteIp = false
        });

        await middleware.InvokeAsync(ctx,
            RuleEngine(allows: false).Object,
            settingsMock.Object,
            ContentUrlResolver().Object);

        Assert.True(nextCalled);
    }
}
