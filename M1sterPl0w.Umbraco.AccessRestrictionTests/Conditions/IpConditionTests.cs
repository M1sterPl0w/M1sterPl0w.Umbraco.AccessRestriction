using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestrictionTests.Helpers;

namespace M1sterPl0w.Umbraco.AccessRestrictionTests.Conditions;

public class IpConditionTests
{
    // ── Matching via HttpContext.Items (set by middleware) ───────────────────

    [Fact]
    public void IsMatch_IpInAllowedList_ReturnsTrue()
    {
        var condition = new IpCondition { AllowedIps = ["192.168.1.1"] };
        var ctx = new TestHttpContextBuilder().WithIpItems("192.168.1.1").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_IpNotInAllowedList_ReturnsFalse()
    {
        var condition = new IpCondition { AllowedIps = ["192.168.1.1"] };
        var ctx = new TestHttpContextBuilder().WithIpItems("10.0.0.1").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_IsCaseInsensitive_ForIPv6()
    {
        // IPv6 addresses can appear in different cases depending on the OS/proxy
        var condition = new IpCondition { AllowedIps = ["2001:DB8::1"] };
        var ctx = new TestHttpContextBuilder().WithIpItems("2001:db8::1").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    // ── Fallback to RemoteIpAddress when Items not populated ────────────────

    [Fact]
    public void IsMatch_FallsBackToRemoteIpAddress_WhenNoItemsSet()
    {
        var condition = new IpCondition { AllowedIps = ["192.168.1.1"] };
        var ctx = new TestHttpContextBuilder().WithRemoteIp("192.168.1.1").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_FallsBackToRemoteIpAddress_NoMatch()
    {
        var condition = new IpCondition { AllowedIps = ["192.168.1.1"] };
        var ctx = new TestHttpContextBuilder().WithRemoteIp("10.0.0.1").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    // ── Multiple IPs (considerRemoteIp=true scenario) ────────────────────────

    [Fact]
    public void IsMatch_MultipleIpsInItems_MatchesIfAnyMatches()
    {
        // Middleware puts both header IP and socket IP when "Also consider direct IP" is on
        var condition = new IpCondition { AllowedIps = ["10.0.0.5"] };
        var ctx = new TestHttpContextBuilder()
            .WithIpItems("1.2.3.4", "10.0.0.5") // header + remote
            .Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_MultipleIpsInItems_NoneMatch_ReturnsFalse()
    {
        var condition = new IpCondition { AllowedIps = ["172.16.0.1"] };
        var ctx = new TestHttpContextBuilder()
            .WithIpItems("1.2.3.4", "10.0.0.5")
            .Build();

        Assert.False(condition.IsMatch(ctx));
    }

    // ── Edge cases ──────────────────────────────────────────────────────────

    [Fact]
    public void IsMatch_EmptyAllowedIps_AlwaysReturnsFalse()
    {
        var condition = new IpCondition { AllowedIps = [] };
        var ctx = new TestHttpContextBuilder().WithIpItems("192.168.1.1").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_NoIpResolvedAtAll_ReturnsFalse()
    {
        // No Items set, no RemoteIpAddress → falls back to [""] which matches nothing
        var condition = new IpCondition { AllowedIps = ["192.168.1.1"] };
        var ctx = new TestHttpContextBuilder().Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_CidrNotation_IsNotSupportedSoNoMatch()
    {
        // CIDR notation is stored as a literal string; "192.168.1.0/24" ≠ "192.168.1.50"
        var condition = new IpCondition { AllowedIps = ["192.168.1.0/24"] };
        var ctx = new TestHttpContextBuilder().WithIpItems("192.168.1.50").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_Localhost_IPv4_ReturnsTrue()
    {
        var condition = new IpCondition { AllowedIps = ["127.0.0.1"] };
        var ctx = new TestHttpContextBuilder().WithIpItems("127.0.0.1").Build();

        Assert.True(condition.IsMatch(ctx));
    }
}
