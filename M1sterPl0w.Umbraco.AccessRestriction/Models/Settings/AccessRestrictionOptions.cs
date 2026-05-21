namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class AccessRestrictionOptions
    {
        public const string SectionName = "AccessRestriction";

        public string? IpHeader { get; set; }

        public List<StaticRuleEntry> Rules { get; set; } = [];
    }
}
