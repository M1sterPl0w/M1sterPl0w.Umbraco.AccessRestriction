using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestrictionTests.Helpers;

namespace M1sterPl0w.Umbraco.AccessRestrictionTests.Conditions;

public class PathConditionTests
{
    // ── Exact match ─────────────────────────────────────────────────────────

    [Fact]
    public void IsMatch_ExactPath_ReturnsTrue()
    {
        var condition = new PathCondition { Paths = ["/admin"] };
        var ctx = new TestHttpContextBuilder().WithPath("/admin").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_ExactPath_DifferentPath_ReturnsFalse()
    {
        var condition = new PathCondition { Paths = ["/admin"] };
        var ctx = new TestHttpContextBuilder().WithPath("/home").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    // ── Prefix match ─────────────────────────────────────────────────────────

    [Fact]
    public void IsMatch_SubPath_ReturnsTrue()
    {
        var condition = new PathCondition { Paths = ["/admin"] };
        var ctx = new TestHttpContextBuilder().WithPath("/admin/users/edit").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_SubPath_ImmediateChild_ReturnsTrue()
    {
        var condition = new PathCondition { Paths = ["/admin"] };
        var ctx = new TestHttpContextBuilder().WithPath("/admin/dashboard").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    // ── Case insensitivity ──────────────────────────────────────────────────

    [Fact]
    public void IsMatch_IsCaseInsensitive()
    {
        var condition = new PathCondition { Paths = ["/Admin"] };
        var ctx = new TestHttpContextBuilder().WithPath("/ADMIN/settings").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    // ── Trailing slash normalisation ─────────────────────────────────────────

    [Fact]
    public void IsMatch_ConfiguredPathWithTrailingSlash_IsTrimmed()
    {
        // "/admin/" in config should still match "/admin" exactly
        var condition = new PathCondition { Paths = ["/admin/"] };
        var ctx = new TestHttpContextBuilder().WithPath("/admin").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_ConfiguredPathWithTrailingSlash_MatchesSubPath()
    {
        var condition = new PathCondition { Paths = ["/admin/"] };
        var ctx = new TestHttpContextBuilder().WithPath("/admin/users").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    // ── Segment boundary — no partial segment match ──────────────────────────

    [Fact]
    public void IsMatch_SimilarButLongerPath_ReturnsFalse()
    {
        // "/admin-panel" must NOT match the "/admin" rule (it crosses a segment boundary)
        var condition = new PathCondition { Paths = ["/admin"] };
        var ctx = new TestHttpContextBuilder().WithPath("/admin-panel").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_PrefixWithoutSlash_ReturnsFalse()
    {
        // "/administrator" must NOT match "/admin"
        var condition = new PathCondition { Paths = ["/admin"] };
        var ctx = new TestHttpContextBuilder().WithPath("/administrator").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    // ── Multiple paths ───────────────────────────────────────────────────────

    [Fact]
    public void IsMatch_MultiplePaths_MatchesAny()
    {
        var condition = new PathCondition { Paths = ["/shop", "/checkout"] };
        var ctx = new TestHttpContextBuilder().WithPath("/checkout/step2").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_MultiplePaths_NoneMatch_ReturnsFalse()
    {
        var condition = new PathCondition { Paths = ["/shop", "/checkout"] };
        var ctx = new TestHttpContextBuilder().WithPath("/blog/post-1").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    // ── Edge cases ───────────────────────────────────────────────────────────

    [Fact]
    public void IsMatch_EmptyPaths_AlwaysReturnsFalse()
    {
        var condition = new PathCondition { Paths = [] };
        var ctx = new TestHttpContextBuilder().WithPath("/admin").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_RootPath_MatchesAllPaths()
    {
        // "/" configured → every request path starts with "/"
        var condition = new PathCondition { Paths = ["/"] };
        var ctx = new TestHttpContextBuilder().WithPath("/any/deeply/nested/path").Build();

        Assert.True(condition.IsMatch(ctx));
    }
}
