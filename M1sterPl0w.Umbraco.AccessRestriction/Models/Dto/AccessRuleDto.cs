using M1sterPl0w.Umbraco.AccessRestriction.Constant;

namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class AccessRuleDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool RequireAll { get; set; } = true;
        
        public string Result { get; set; } = AccessConstants.Allow;
        
        public int SortOrder { get; set; }
        
        public bool CanDelete { get; set; } = true;
        
        public string? CreatedBy { get; set; }
        
        public DateTime? CreatedDate { get; set; }
        
        public List<ConditionDto> Conditions { get; set; } = [];
    }
}
