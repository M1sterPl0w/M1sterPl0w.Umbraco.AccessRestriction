using M1sterPl0w.Umbraco.AccessRestriction.Middleware;
using M1sterPl0w.Umbraco.AccessRestriction.Migrations;
using M1sterPl0w.Umbraco.AccessRestriction.Models;
using M1sterPl0w.Umbraco.AccessRestriction.RuleEngine;
using M1sterPl0w.Umbraco.AccessRestriction.Services;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace M1sterPl0w.Umbraco.AccessRestriction.Composers
{
    public class M1sterPl0wUmbracoAccessRestrictionApiComposer : IComposer
    {
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

            // Configured transformers run after Umbraco's own, so AccessRestrictionOperationIdTransformer
            // overrides the default operation IDs.
            builder.AddBackOfficeOpenApiDocument(Constants.ApiName, document => document
                .WithTitle("M1sterPl0w Umbraco Access Restriction Backoffice API")
                .WithBackOfficeAuthentication()
                .ConfigureOpenApiOptions(options => options.AddOperationTransformer<AccessRestrictionOperationIdTransformer>()));
        }
    }
}
