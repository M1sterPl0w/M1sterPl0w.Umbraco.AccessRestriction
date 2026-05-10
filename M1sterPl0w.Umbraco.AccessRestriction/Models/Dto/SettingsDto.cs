namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public sealed class SettingsDto
    {
        public required bool Enabled { get; init; }

        /// <remarks>When set, the middleware reads the client IP from this HTTP request header instead of the connection remote IP.</remarks>
        public required string? IpHeader { get; init; }

        /// <remarks>True when IpHeader is set via appsettings — the field is read-only at runtime.</remarks>
        public required bool IsIpHeaderForced { get; init; }

        /// <remarks>When true and an IP header is configured, the raw socket remote IP is also checked alongside the header IP.</remarks>
        public required bool ConsiderRemoteIp { get; init; }

        /// <remarks>HTTP status code returned when access is denied. Defaults to 403.</remarks>
        public int DenyStatusCode { get; init; } = 403;

        /// <remarks>Optional Umbraco content node key. When set, the denied user is redirected to this node's URL.</remarks>
        public Guid? DenyContentNodeKey { get; init; }
    }
}
