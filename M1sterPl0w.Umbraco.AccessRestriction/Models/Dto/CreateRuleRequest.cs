namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class CreateRuleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequireAll { get; set; } = true;
        /// <summary>"Allow" or "Deny"</summary>
        public string Result { get; set; } = "Allow";
    }
}
