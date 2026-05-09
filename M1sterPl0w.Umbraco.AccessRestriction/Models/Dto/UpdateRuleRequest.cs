namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class UpdateRuleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequireAll { get; set; } = true;
        /// <summary>"Allow" or "Deny"</summary>
        public string Result { get; set; } = "Allow";
        public int SortOrder { get; set; }
    }
}
