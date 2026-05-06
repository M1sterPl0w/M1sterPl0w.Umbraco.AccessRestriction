namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public sealed class SettingsDto
    {
        public required bool Enabled { get; init; }

        /// <remarks>True when IP addresses are configured via appsettings — the restriction cannot be disabled at runtime.</remarks>
        public required bool IsEnabledForced { get; init; }

        /// <remarks>When set, the middleware reads the client IP from this HTTP request header instead of the connection remote IP.</remarks>
        public required string? IpHeader { get; init; }

        /// <remarks>True when IpHeader is set via appsettings — the field is read-only at runtime.</remarks>
        public required bool IsIpHeaderForced { get; init; }
    }
}
