using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestrictionTests.Helpers;

namespace M1sterPl0w.Umbraco.AccessRestrictionTests.Conditions;

public class UserGroupConditionTests
{
    // ── Authenticated user ──────────────────────────────────────────────────

    [Fact]
    public void IsMatch_AuthenticatedUserInMatchingGroup_ReturnsTrue()
    {
        var condition = new UserGroupCondition { Groups = ["Editors"] };
        var ctx = new TestHttpContextBuilder().WithAuthenticatedUser("Editors").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_AuthenticatedUserNotInGroup_ReturnsFalse()
    {
        var condition = new UserGroupCondition { Groups = ["Editors"] };
        var ctx = new TestHttpContextBuilder().WithAuthenticatedUser("Writers").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    // ── Unauthenticated user ────────────────────────────────────────────────

    [Fact]
    public void IsMatch_UnauthenticatedUser_ReturnsFalse()
    {
        var condition = new UserGroupCondition { Groups = ["Editors"] };
        var ctx = new TestHttpContextBuilder().WithUnauthenticatedUser().Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_DefaultContext_NoUser_ReturnsFalse()
    {
        // Default DefaultHttpContext has no authenticated user
        var condition = new UserGroupCondition { Groups = ["Editors"] };
        var ctx = new TestHttpContextBuilder().Build();

        Assert.False(condition.IsMatch(ctx));
    }

    // ── Multiple groups (OR logic) ──────────────────────────────────────────

    [Fact]
    public void IsMatch_UserInOneOfMultipleGroups_ReturnsTrue()
    {
        var condition = new UserGroupCondition { Groups = ["Editors", "Admins", "Writers"] };
        var ctx = new TestHttpContextBuilder().WithAuthenticatedUser("Admins").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_UserInNoneOfMultipleGroups_ReturnsFalse()
    {
        var condition = new UserGroupCondition { Groups = ["Editors", "Admins"] };
        var ctx = new TestHttpContextBuilder().WithAuthenticatedUser("Writers").Build();

        Assert.False(condition.IsMatch(ctx));
    }

    [Fact]
    public void IsMatch_UserInMultipleRoles_OneMatches_ReturnsTrue()
    {
        // User has two roles; condition only requires one of them
        var condition = new UserGroupCondition { Groups = ["Admins"] };
        var ctx = new TestHttpContextBuilder().WithAuthenticatedUser("Editors", "Admins").Build();

        Assert.True(condition.IsMatch(ctx));
    }

    // ── Edge cases ───────────────────────────────────────────────────────────

    [Fact]
    public void IsMatch_EmptyGroups_AlwaysReturnsFalse()
    {
        var condition = new UserGroupCondition { Groups = [] };
        var ctx = new TestHttpContextBuilder().WithAuthenticatedUser("Editors").Build();

        Assert.False(condition.IsMatch(ctx));
    }
}
