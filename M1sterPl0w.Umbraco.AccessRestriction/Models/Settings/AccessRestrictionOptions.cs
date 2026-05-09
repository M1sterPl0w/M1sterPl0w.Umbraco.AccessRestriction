namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class AccessRestrictionOptions
    {
        public const string SectionName = "AccessRestriction";

        /// <summary>When set, the middleware reads the client IP from this HTTP header instead of the connection remote IP. E.g. "X-Forwarded-For" or "X-Real-IP".</summary>
        public string? IpHeader { get; set; }

        /// <summary>Static access rules defined in appsettings. These are read-only and cannot be modified from the backoffice UI.</summary>
        public List<StaticRuleEntry> Rules { get; set; } = [];
    }

    public class StaticRuleEntry
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequireAll { get; set; } = true;
        /// <summary>"Allow" or "Deny"</summary>
        public string Result { get; set; } = "Allow";
        public int SortOrder { get; set; } = 0;
        public List<StaticConditionEntry> Conditions { get; set; } = [];
    }

    public class StaticConditionEntry
    {
        /// <summary>"Ip", "Path", or "UserGroup"</summary>
        public string Type { get; set; } = string.Empty;
        public List<string> Values { get; set; } = [];
    }
}
