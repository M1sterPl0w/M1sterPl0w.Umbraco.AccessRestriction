namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class AccessRestrictionOptions
    {
        public const string SectionName = "AccessRestriction";

        public List<StaticIpAddressEntry> IpAddresses { get; set; } = [];
        public List<StaticPathEntry> Paths { get; set; } = [];
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
