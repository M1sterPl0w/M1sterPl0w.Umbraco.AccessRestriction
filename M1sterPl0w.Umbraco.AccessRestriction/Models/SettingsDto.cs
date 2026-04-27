namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class SettingsDto
    {
        public bool Enabled { get; set; }
        /// <summary>True when IP addresses are configured via appsettings — the restriction cannot be disabled at runtime.</summary>
        public bool IsEnabledForced { get; set; }
    }
}
