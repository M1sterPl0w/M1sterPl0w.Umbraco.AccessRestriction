using M1sterPl0w.Umbraco.AccessRestriction.Models;
using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestriction.Services;
using M1sterPl0w.Umbraco.AccessRestrictionTests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace M1sterPl0w.Umbraco.AccessRestrictionTests.RuleEngine;

public class AccessRuleEngineTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AccessRuleEngine CreateEngine(IReadOnlyList<AccessRuleDto> rules)
    {
        var repo = new Mock<IRuleRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(rules);
        var logger = new Mock<ILogger<AccessRuleEngine>>();
        return new AccessRuleEngine(repo.Object, logger.Object);
    }

    private static ConditionDto IP(params string[] ips) =>
        new() { Type = "Ip", Values = [..ips] };

    private static ConditionDto Path(params string[] paths) =>
        new() { Type = "Path", Values = [..paths] };

    private static AccessRuleDto AllowRule(int sortOrder, params ConditionDto[] conditions) =>
        new() { Id = sortOrder, Name = $"Allow-{sortOrder}", Result = "Allow", RequireAll = true, SortOrder = sortOrder, Conditions = [..conditions] };

    private static AccessRuleDto DenyRule(int sortOrder, params ConditionDto[] conditions) =>
        new() { Id = sortOrder + 100, Name = $"Deny-{sortOrder}", Result = "Deny", RequireAll = true, SortOrder = sortOrder, Conditions = [..conditions] };

    // ── Default allow (no rules / no match) ──────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_NoRulesConfigured_AllowsRequest()
    {
        var engine = CreateEngine([]);
        var ctx = new TestHttpContextBuilder().Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    [Fact]
    public async Task EvaluateAsync_NoRuleMatches_DefaultAllowsRequest()
    {
        var engine = CreateEngine([DenyRule(1, IP("1.2.3.4"))]);
        var ctx = new TestHttpContextBuilder().WithIpItems("99.99.99.99").Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    [Fact]
    public async Task EvaluateAsync_AllRulesHaveNoConditions_DefaultAllowsRequest()
    {
        // Rules with no conditions are skipped
        var engine = CreateEngine([AllowRule(1), DenyRule(2)]);
        var ctx = new TestHttpContextBuilder().Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    // ── Allow and Deny ───────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_MatchingAllowRule_ReturnsTrue()
    {
        var engine = CreateEngine([AllowRule(1, IP("1.2.3.4"))]);
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    [Fact]
    public async Task EvaluateAsync_MatchingDenyRule_ReturnsFalse()
    {
        var engine = CreateEngine([DenyRule(1, IP("1.2.3.4"))]);
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").Build();

        Assert.False(await engine.EvaluateAsync(ctx));
    }

    // ── Sort order / first match wins ────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_FirstMatchWins_DenyBeforeAllow()
    {
        var engine = CreateEngine([
            DenyRule(1,  IP("1.2.3.4")),
            AllowRule(2, IP("1.2.3.4"))
        ]);
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").Build();

        Assert.False(await engine.EvaluateAsync(ctx));
    }

    [Fact]
    public async Task EvaluateAsync_FirstMatchWins_AllowBeforeDeny()
    {
        var engine = CreateEngine([
            AllowRule(1, IP("1.2.3.4")),
            DenyRule(2,  IP("1.2.3.4"))
        ]);
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    [Fact]
    public async Task EvaluateAsync_RulesEvaluatedBySortOrder_NotRegistrationOrder()
    {
        // Deny has higher sort number but should still be evaluated based on SortOrder
        var engine = CreateEngine([
            AllowRule(10, IP("1.2.3.4")),  // registered first, but SortOrder=10
            DenyRule(1,   IP("1.2.3.4"))   // registered second, but SortOrder=1 → evaluated first
        ]);
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").Build();

        Assert.False(await engine.EvaluateAsync(ctx));
    }

    // ── RequireAll = true (AND) ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_RequireAll_BothConditionsMatch_RuleFires()
    {
        var rule = new AccessRuleDto
        {
            Id = 1, Name = "AND rule", Result = "Deny", RequireAll = true, SortOrder = 1,
            Conditions = [IP("1.2.3.4"), Path("/admin")]
        };
        var engine = CreateEngine([rule]);
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").WithPath("/admin").Build();

        Assert.False(await engine.EvaluateAsync(ctx));
    }

    [Fact]
    public async Task EvaluateAsync_RequireAll_OneConditionFails_RuleSkipped()
    {
        var rule = new AccessRuleDto
        {
            Id = 1, Name = "AND rule", Result = "Deny", RequireAll = true, SortOrder = 1,
            Conditions = [IP("1.2.3.4"), Path("/admin")]
        };
        var engine = CreateEngine([rule]);
        // IP matches but path doesn't → rule does NOT fire → default allow
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").WithPath("/home").Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    // ── RequireAll = false (OR) ───────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_RequireAny_OneConditionMatches_RuleFires()
    {
        var rule = new AccessRuleDto
        {
            Id = 1, Name = "OR rule", Result = "Deny", RequireAll = false, SortOrder = 1,
            Conditions = [IP("1.2.3.4"), Path("/admin")]
        };
        var engine = CreateEngine([rule]);
        // IP does NOT match, path DOES match → rule fires (OR logic)
        var ctx = new TestHttpContextBuilder().WithIpItems("99.99.99.99").WithPath("/admin").Build();

        Assert.False(await engine.EvaluateAsync(ctx));
    }

    [Fact]
    public async Task EvaluateAsync_RequireAny_NoConditionMatches_RuleSkipped()
    {
        var rule = new AccessRuleDto
        {
            Id = 1, Name = "OR rule", Result = "Deny", RequireAll = false, SortOrder = 1,
            Conditions = [IP("1.2.3.4"), Path("/admin")]
        };
        var engine = CreateEngine([rule]);
        // Neither IP nor path matches → rule doesn't fire → default allow
        var ctx = new TestHttpContextBuilder().WithIpItems("99.99.99.99").WithPath("/home").Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    // ── Unknown condition type → AlwaysDenyCondition ─────────────────────────

    [Fact]
    public async Task EvaluateAsync_UnknownConditionType_RuleNeverMatches_DefaultAllow()
    {
        // "GeoBlock" is not a recognised type → AlwaysDenyCondition (IsMatch=false)
        // → rule never fires → falls through to default allow
        var rule = new AccessRuleDto
        {
            Id = 1, Name = "Unknown", Result = "Deny", RequireAll = true, SortOrder = 1,
            Conditions = [new ConditionDto { Type = "GeoBlock", Values = ["NL"] }]
        };
        var engine = CreateEngine([rule]);
        var ctx = new TestHttpContextBuilder().Build();

        Assert.True(await engine.EvaluateAsync(ctx));
    }

    // ── Mixed: empty-condition rule is skipped, later rule fires ─────────────

    [Fact]
    public async Task EvaluateAsync_EmptyConditionRuleSkipped_NextRuleEvaluated()
    {
        var engine = CreateEngine([
            new AccessRuleDto { Id = 1, Name = "Empty", Result = "Allow", RequireAll = true, SortOrder = 1, Conditions = [] },
            DenyRule(2, IP("1.2.3.4"))
        ]);
        var ctx = new TestHttpContextBuilder().WithIpItems("1.2.3.4").Build();

        // Empty rule at SortOrder=1 is skipped; Deny at SortOrder=2 fires
        Assert.False(await engine.EvaluateAsync(ctx));
    }

    // ── Complex scenario ─────────────────────────────────────────────────────

    [Fact]
    public async Task EvaluateAsync_AllowOfficeIp_ThenDenyEverythingElse()
    {
        // Classic pattern: whitelist an IP for /admin, deny all other /admin access
        var allowOffice = new AccessRuleDto
        {
            Id = 1, Name = "Office allow", Result = "Allow", RequireAll = true, SortOrder = 1,
            Conditions = [IP("10.0.0.1"), Path("/admin")]
        };
        var denyAdmin = new AccessRuleDto
        {
            Id = 2, Name = "Block /admin", Result = "Deny", RequireAll = true, SortOrder = 2,
            Conditions = [Path("/admin")]
        };
        var engine = CreateEngine([allowOffice, denyAdmin]);

        // Office IP accessing /admin → allowed by rule 1
        var officeCtx = new TestHttpContextBuilder().WithIpItems("10.0.0.1").WithPath("/admin").Build();
        Assert.True(await engine.EvaluateAsync(officeCtx));

        // Random IP accessing /admin → denied by rule 2
        var randomCtx = new TestHttpContextBuilder().WithIpItems("5.5.5.5").WithPath("/admin").Build();
        Assert.False(await engine.EvaluateAsync(randomCtx));

        // Random IP accessing /home → neither rule matches → default allow
        var homeCtx = new TestHttpContextBuilder().WithIpItems("5.5.5.5").WithPath("/home").Build();
        Assert.True(await engine.EvaluateAsync(homeCtx));
    }
}
