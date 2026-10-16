using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace M1sterPl0w.Umbraco.AccessRestriction.Composers
{
    /// <summary>
    /// Sets the operation ID of this package's API actions to the action name, matching the
    /// former Umbraco 17 <c>CustomOperationHandler</c> (kept on release/v17) so the generated TypeScript client is identical on both majors.
    /// </summary>
    public class AccessRestrictionOperationIdTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.Description.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
                && controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(
                    "M1sterPl0w.Umbraco.AccessRestriction.Controllers",
                    StringComparison.InvariantCultureIgnoreCase) is true)
            {
                operation.OperationId = controllerActionDescriptor.ActionName;
            }

            return Task.CompletedTask;
        }
    }
}
