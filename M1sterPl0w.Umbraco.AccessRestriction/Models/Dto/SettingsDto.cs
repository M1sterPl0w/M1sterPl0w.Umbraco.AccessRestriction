namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public sealed class SettingsDto
    {
        public required bool Enabled { get; init; }

        public required string? IpHeader { get; init; }

        public required bool IsIpHeaderForced { get; init; }

        public required bool ConsiderRemoteIp { get; init; }

        public int DenyStatusCode { get; init; } = 403;

        public Guid? DenyContentNodeKey { get; init; }
    }
}
