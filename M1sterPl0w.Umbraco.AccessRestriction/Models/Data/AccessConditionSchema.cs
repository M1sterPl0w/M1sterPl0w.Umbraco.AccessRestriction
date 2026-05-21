using NPoco;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    [TableName(AccessConditionSchema.TableName)]
    [PrimaryKey("Id", AutoIncrement = true)]
    [ExplicitColumns]
    public class AccessConditionSchema
    {
        public const string TableName = "AccessRestrictionConditions";

        [Column("Id")]
        public int Id { get; set; }

        [Column("RuleId")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public int RuleId { get; set; }

        [Column("ConditionType")]
        [Length(50)]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string ConditionType { get; set; } = string.Empty;

        [Column("Values")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        public string Values { get; set; } = "[]";
    }
}
