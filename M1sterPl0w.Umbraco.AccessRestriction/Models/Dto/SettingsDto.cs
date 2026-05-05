namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public sealed class SettingsDto
    {
        public required bool Enabled { get; init; }

        /// <remarks>True when IP addresses are configured via appsettings — the restriction cannot be disabled at runtime.</remarks>
        public required bool IsEnabledForced { get; init; }
    }
}
