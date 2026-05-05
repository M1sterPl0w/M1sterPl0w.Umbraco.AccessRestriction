using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    [TableName(RestrictedPathSchema.TableName)]
    [PrimaryKey("Path", AutoIncrement = false)]
    [ExplicitColumns]
    public class RestrictedPathSchema
    {
        public const string TableName = "AccessRestrictionPaths";

        [Column("Path")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Path { get; set; } = string.Empty;

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
