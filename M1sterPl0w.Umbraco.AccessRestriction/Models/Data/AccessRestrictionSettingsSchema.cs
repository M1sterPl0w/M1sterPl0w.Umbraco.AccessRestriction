using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    [TableName(AccessRestrictionSettingsSchema.TableName)]
    [PrimaryKey("Key", AutoIncrement = false)]
    [ExplicitColumns]
    public sealed class AccessRestrictionSettingsSchema
    {
        public const string TableName = "AccessRestrictionSettings";

        public const string KeyEnabled = "Enabled";
        public const string KeyIpHeader = "IpHeader";
        public const string KeyConsiderRemoteIp = "ConsiderRemoteIp";
        public const string KeyDenyStatusCode = "DenyStatusCode";
        public const string KeyDenyContentNodeKey = "DenyContentNodeKey";

        [Column("Key")]
        [Length(100)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public required string Key { get; init; } = string.Empty;

        [Column("Value")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public required string? Value { get; init; }
    }
}
