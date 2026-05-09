using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    [TableName(AccessRuleSchema.TableName)]
    [PrimaryKey("Id", AutoIncrement = true)]
    [ExplicitColumns]
    public class AccessRuleSchema
    {
        public const string TableName = "AccessRestrictionRules";

        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        [Length(200)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Name { get; set; } = string.Empty;

        [Column("Description")]
        [Length(500)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? Description { get; set; }

        [Column("RequireAll")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public bool RequireAll { get; set; } = true;

        [Column("Result")]
        [Length(10)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Result { get; set; } = "Allow";

        [Column("SortOrder")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public int SortOrder { get; set; }

        [Column("CreatedDate")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? CreatedDate { get; set; }

        [Column("CreatedBy")]
        [Length(200)]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string? CreatedBy { get; set; }
    }
}
