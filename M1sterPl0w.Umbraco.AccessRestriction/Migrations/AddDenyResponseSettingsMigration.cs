using M1sterPl0w.Umbraco.AccessRestriction.Models;
using Umbraco.Cms.Infrastructure.Migrations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Migrations
{
    public class AddDenyResponseSettingsMigration : AsyncMigrationBase
    {
        public AddDenyResponseSettingsMigration(IMigrationContext context) : base(context)
        {
        }

        protected override Task MigrateAsync()
        {
            var rows = Database.Fetch<AccessRestrictionSettingsSchema>();

            if (!rows.Any(r => string.Equals(r.Key, AccessRestrictionSettingsSchema.KeyDenyStatusCode, StringComparison.OrdinalIgnoreCase)))
            {
                Database.Insert(new AccessRestrictionSettingsSchema
                {
                    Key   = AccessRestrictionSettingsSchema.KeyDenyStatusCode,
                    Value = "403"
                });
            }

            return Task.CompletedTask;
        }
    }
}
