namespace M1sterPl0w.Umbraco.AccessRestriction.Models
{
    public class AllowedIpAddressDto
    {
        public string IpAddress { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public bool CanDelete { get; set; } = true;
    }

    public class CreateAllowedIpAddressRequest
    {
        public string IpAddress { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
