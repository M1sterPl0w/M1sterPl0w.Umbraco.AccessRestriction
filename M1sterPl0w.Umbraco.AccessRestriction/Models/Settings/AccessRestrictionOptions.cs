namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class AccessRestrictionOptions
    {
        public const string SectionName = "AccessRestriction";

        public List<StaticIpAddressEntry> IpAddresses { get; set; } = [];
        public List<StaticPathEntry> Paths { get; set; } = [];

        /// <summary>When set, the middleware reads the client IP from this HTTP header instead of the connection remote IP. E.g. "X-Forwarded-For" or "X-Real-IP".</summary>
        public string? IpHeader { get; set; }
    }

    public class StaticIpAddressEntry
    {
        public string IpAddress { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class StaticPathEntry
    {
        public string Path { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
