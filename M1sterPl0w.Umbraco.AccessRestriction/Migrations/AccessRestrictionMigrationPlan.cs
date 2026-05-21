using Umbraco.Cms.Infrastructure.Migrations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Migrations
{
    public class AccessRestrictionMigrationPlan : MigrationPlan
    {
        public AccessRestrictionMigrationPlan() : base("AccessRestriction")
        {
            From(string.Empty)
                .To<InitialMigration>("1.0")
                .To<AddDenyResponseSettingsMigration>("1.1");
        }
    }
}
