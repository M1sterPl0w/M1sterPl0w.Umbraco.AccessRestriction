using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace M1sterPl0w.Umbraco.AccessRestriction.Migrations
{
    public class RunAccessRestrictionMigrationHandler : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
    {
        private readonly IMigrationPlanExecutor _migrationPlanExecutor;
        private readonly ICoreScopeProvider _coreScopeProvider;
        private readonly IKeyValueService _keyValueService;
        private readonly IRuntimeState _runtimeState;

        public RunAccessRestrictionMigrationHandler(
            IMigrationPlanExecutor migrationPlanExecutor,
            ICoreScopeProvider coreScopeProvider,
            IKeyValueService keyValueService,
            IRuntimeState runtimeState)
        {
            _migrationPlanExecutor = migrationPlanExecutor;
            _coreScopeProvider = coreScopeProvider;
            _keyValueService = keyValueService;
            _runtimeState = runtimeState;
        }

        public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
        {
            if (_runtimeState.Level < RuntimeLevel.Run)
            {
                return;
            }

            var plan = new AccessRestrictionMigrationPlan();
            var upgrader = new Upgrader(plan);
            
            await upgrader.ExecuteAsync(_migrationPlanExecutor, _coreScopeProvider, _keyValueService);
        }
    }
}
