using Asp.Versioning;
using M1sterPl0w.Umbraco.AccessRestriction.Middleware;
using M1sterPl0w.Umbraco.AccessRestriction.Migrations;
using M1sterPl0w.Umbraco.AccessRestriction.Models;
using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestriction.Services;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Api.Common.OpenApi;
#if UMBRACO_18
using Umbraco.Cms.Api.Common.DependencyInjection;
#else
using Swashbuckle.AspNetCore.SwaggerGen;
#endif

namespace M1sterPl0w.Umbraco.AccessRestriction.Composers
{
    public class M1sterPl0wUmbracoAccessRestrictionApiComposer : IComposer
    {
        private const string ApiTitle = "M1sterPl0w Umbraco Access Restriction Backoffice API";

        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.Configure<AccessRestrictionOptions>(builder.Config.GetSection(AccessRestrictionOptions.SectionName));
            builder.Services.AddTransient<Microsoft.AspNetCore.Hosting.IStartupFilter, AccessRestrictionStartupFilter>();

            // Repositories
            builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
            builder.Services.AddScoped<IRuleRepository, RuleRepository>();

            // Rule engine
            builder.Services.AddScoped<IAccessRuleEngine, AccessRuleEngine>();

            // Content URL resolver (used by middleware to redirect to a content node on deny)
            builder.Services.AddScoped<IContentUrlResolver, UmbracoContentUrlResolver>();

            // Migration handler
            builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunAccessRestrictionMigrationHandler>();

#if UMBRACO_18
            // Umbraco 18 uses Microsoft.AspNetCore.OpenApi. Configured transformers run after Umbraco's own,
            // so AccessRestrictionOperationIdTransformer overrides the default operation IDs.
            builder.AddBackOfficeOpenApiDocument(Constants.ApiName, document => document
                .WithTitle(ApiTitle)
                .WithBackOfficeAuthentication()
                .ConfigureOpenApiOptions(options => options.AddOperationTransformer<AccessRestrictionOperationIdTransformer>()));
#else
            builder.Services.AddSingleton<IOperationIdHandler, CustomOperationHandler>();

            builder.Services.Configure<SwaggerGenOptions>(opt =>
            {
                opt.SwaggerDoc(Constants.ApiName, new OpenApiInfo
                {
                    Title = ApiTitle,
                    Version = "1.0"
                });

                opt.OperationFilter<M1sterPl0wUmbracoAccessRestrictionOperationSecurityFilter>();
            });
#endif
        }

#if !UMBRACO_18
        public class M1sterPl0wUmbracoAccessRestrictionOperationSecurityFilter : BackOfficeSecurityRequirementsOperationFilterBase
        {
            protected override string ApiName => Constants.ApiName;
        }

        public class CustomOperationHandler : OperationIdHandler
        {
            public CustomOperationHandler(IOptions<ApiVersioningOptions> apiVersioningOptions) : base(apiVersioningOptions)
            {
            }

            protected override bool CanHandle(ApiDescription apiDescription, ControllerActionDescriptor controllerActionDescriptor)
            {
                return controllerActionDescriptor.ControllerTypeInfo.Namespace?.StartsWith(
                    "M1sterPl0w.Umbraco.AccessRestriction.Controllers",
                    StringComparison.InvariantCultureIgnoreCase) is true;
            }

            public override string Handle(ApiDescription apiDescription)
                => $"{apiDescription.ActionDescriptor.RouteValues["action"]}";
        }
#endif
    }
}
