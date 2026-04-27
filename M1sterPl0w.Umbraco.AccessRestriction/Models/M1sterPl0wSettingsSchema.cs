using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    [TableName(AccessRestrictionSettingsSchema.TableName)]
    [PrimaryKey("Key", AutoIncrement = false)]
    [ExplicitColumns]
    public class AccessRestrictionSettingsSchema
    {
        public const string TableName = "AccessRestrictionSettings";

        public const string KeyEnabled = "Enabled";

        [Column("Key")]
        [Length(100)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Key { get; set; } = string.Empty;

        [Column("Value")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? Value { get; set; }
    }
}
