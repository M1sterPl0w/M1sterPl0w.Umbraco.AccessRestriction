using M1sterPl0w.Umbraco.AccessRestriction.Models;
using Umbraco.Cms.Infrastructure.Migrations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Migrations
{
    public class InitialMigration : MigrationBase
    {
        public InitialMigration(IMigrationContext context) : base(context)
        {
        }

        protected override void Migrate()
        {
            if (!TableExists(AllowedIpAddressSchema.TableName))
            {
                Create.Table(AllowedIpAddressSchema.TableName)
                    .WithColumn("IpAddress").AsString(45).PrimaryKey().NotNullable()
                    .WithColumn("Description").AsString(500).Nullable()
                    .WithColumn("CreatedDate").AsDateTime().Nullable()
                    .WithColumn("CreatedBy").AsString(200).Nullable()
                    .Do();
            }

            if (!TableExists(AccessRestrictionSettingsSchema.TableName))
            {
                Create.Table(AccessRestrictionSettingsSchema.TableName)
                    .WithColumn("Key").AsString(100).PrimaryKey().NotNullable()
                    .WithColumn("Value").AsString(500).Nullable()
                    .Do();

                Database.Insert(new AccessRestrictionSettingsSchema
                {
                    Key = AccessRestrictionSettingsSchema.KeyEnabled,
                    Value = "true"
                });
            }

            if (!TableExists(RestrictedPathSchema.TableName))
            {
                Create.Table(RestrictedPathSchema.TableName)
                    .WithColumn("Path").AsString(500).PrimaryKey().NotNullable()
                    .WithColumn("Description").AsString(500).Nullable()
                    .WithColumn("CreatedDate").AsDateTime().Nullable()
                    .WithColumn("CreatedBy").AsString(200).Nullable()
                    .Do();
            }
        }
    }
}
