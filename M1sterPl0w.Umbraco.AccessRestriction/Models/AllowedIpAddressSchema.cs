using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    [TableName(AllowedIpAddressSchema.TableName)]
    [PrimaryKey("IpAddress", AutoIncrement = false)]
    [ExplicitColumns]
    public class AllowedIpAddressSchema
    {
        public const string TableName = "AccessRestrictionIpAddresses";

        [Column("IpAddress")]
        [Length(45)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string IpAddress { get; set; } = string.Empty;

        [Column("Description")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? Description { get; set; }

        [Column("CreatedDate")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? CreatedDate { get; set; }

        [Column("CreatedBy")]
        [Length(200)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? CreatedBy { get; set; }
    }
}
