using M1sterPl0w.Umbraco.AccessRestriction.Models;
using Umbraco.Cms.Infrastructure.Migrations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Migrations
{
    public class AddDenyResponseSettingsMigration : MigrationBase
    {
        public AddDenyResponseSettingsMigration(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
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
        }
    }
}
