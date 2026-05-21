using M1sterPl0w.Umbraco.AccessRestriction.Constants;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class StaticRuleEntry
    {
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool RequireAll { get; set; } = true;

        public string Result { get; set; } = AccessConstants.Allow;
        
        public int SortOrder { get; set; } = 0;
        
        public List<StaticConditionEntry> Conditions { get; set; } = [];
    }
}
