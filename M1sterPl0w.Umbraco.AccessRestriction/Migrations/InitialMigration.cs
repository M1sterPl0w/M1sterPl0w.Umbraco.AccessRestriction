using M1sterPl0w.Umbraco.AccessRestriction.Constants;
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
            // Drop legacy V1 tables if they exist (safe upgrade path)
            if (TableExists("AccessRestrictionIpAddresses"))
                Delete.Table("AccessRestrictionIpAddresses").Do();

            if (TableExists("AccessRestrictionPaths"))
                Delete.Table("AccessRestrictionPaths").Do();

            // Settings (key-value store)
            if (!TableExists(AccessRestrictionSettingsSchema.TableName))
            {
                Create.Table(AccessRestrictionSettingsSchema.TableName)
                    .WithColumn("Key").AsString(100).PrimaryKey().NotNullable()
                    .WithColumn("Value").AsString(500).Nullable()
                    .Do();

                Database.Insert(new AccessRestrictionSettingsSchema
                {
                    Key   = AccessRestrictionSettingsSchema.KeyEnabled,
                    Value = "true"
                });

                Database.Insert(new AccessRestrictionSettingsSchema
                {
                    Key   = AccessRestrictionSettingsSchema.KeyConsiderRemoteIp,
                    Value = "false"
                });
            }

            // Access rules
            if (!TableExists(AccessRuleSchema.TableName))
            {
                Create.Table(AccessRuleSchema.TableName)
                    .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                    .WithColumn("Name").AsString(200).NotNullable()
                    .WithColumn("Description").AsString(500).Nullable()
                    .WithColumn("RequireAll").AsBoolean().NotNullable().WithDefaultValue(true)
                    .WithColumn("Result").AsString(10).NotNullable().WithDefaultValue(AccessConstants.Allow)
                    .WithColumn("SortOrder").AsInt32().NotNullable().WithDefaultValue(0)
                    .WithColumn("CreatedDate").AsDateTime().Nullable()
                    .WithColumn("CreatedBy").AsString(200).Nullable()
                    .Do();
            }

            // Conditions per rule
            if (!TableExists(AccessConditionSchema.TableName))
            {
                Create.Table(AccessConditionSchema.TableName)
                    .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                    .WithColumn("RuleId").AsInt32().NotNullable()
                    .WithColumn("ConditionType").AsString(50).NotNullable()
                    .WithColumn("Values").AsString(int.MaxValue).NotNullable().WithDefaultValue("[]")
                    .Do();
            }
        }
    }
}
