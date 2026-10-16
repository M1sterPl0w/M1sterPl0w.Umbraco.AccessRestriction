#if UMBRACO_18
using System.Reflection;
using M1sterPl0w.Umbraco.AccessRestriction.Composers;
using M1sterPl0w.Umbraco.AccessRestriction.Controllers;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace M1sterPl0w.Umbraco.AccessRestrictionTests.Composers;

public class AccessRestrictionOperationIdTransformerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static OpenApiOperationTransformerContext ContextFor(ActionDescriptor actionDescriptor) => new()
    {
        DocumentName = "test",
        Description = new ApiDescription { ActionDescriptor = actionDescriptor },
        ApplicationServices = Mock.Of<IServiceProvider>()
    };

    private static ControllerActionDescriptor ControllerAction(Type controllerType, string actionName) => new()
    {
        ControllerTypeInfo = controllerType.GetTypeInfo(),
        ActionName = actionName
    };

    private static async Task<string?> TransformAsync(ActionDescriptor actionDescriptor)
    {
        var operation = new OpenApiOperation { OperationId = "GetUmbracoDefault" };
        await new AccessRestrictionOperationIdTransformer()
            .TransformAsync(operation, ContextFor(actionDescriptor), CancellationToken.None);
        return operation.OperationId;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PackageController_OperationIdIsActionName()
    {
        var result = await TransformAsync(ControllerAction(typeof(M1sterPl0wUmbracoAccessRestrictionApiController), "GetRules"));

        Assert.Equal("GetRules", result);
    }

    [Fact]
    public async Task ControllerOutsidePackageNamespace_OperationIdUnchanged()
    {
        var result = await TransformAsync(ControllerAction(typeof(AccessRestrictionOperationIdTransformerTests), "GetRules"));

        Assert.Equal("GetUmbracoDefault", result);
    }

    [Fact]
    public async Task NonControllerAction_OperationIdUnchanged()
    {
        var result = await TransformAsync(new ActionDescriptor());

        Assert.Equal("GetUmbracoDefault", result);
    }
}
#endif
