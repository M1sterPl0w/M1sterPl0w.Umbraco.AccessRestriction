using Asp.Versioning;
using M1sterPl0w.Umbraco.AccessRestriction.Models;
using M1sterPl0w.Umbraco.AccessRestriction.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;

namespace M1sterPl0w.Umbraco.AccessRestriction.Controllers
{
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "M1sterPl0w.Umbraco.AccessRestriction")]
    public class M1sterPl0wUmbracoAccessRestrictionApiController : M1sterPl0wUmbracoAccessRestrictionApiControllerBase
    {
        private readonly IIpAddressRepository _repository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IRestrictedPathRepository _pathsRepository;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        public M1sterPl0wUmbracoAccessRestrictionApiController(
            IIpAddressRepository repository,
            ISettingsRepository settingsRepository,
            IRestrictedPathRepository pathsRepository,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        {
            _repository = repository;
            _settingsRepository = settingsRepository;
            _pathsRepository = pathsRepository;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        [HttpGet("ping")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public string Ping() => "Pong";

        [HttpGet("ipaddresses")]
        [ProducesResponseType<IReadOnlyList<AllowedIpAddressDto>>(StatusCodes.Status200OK)]
        public async Task<IReadOnlyList<AllowedIpAddressDto>> GetIpAddresses()
            => await _repository.GetAllAsync();

        [HttpGet("ipaddresses/mine")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public string GetMyIpAddress()
        {
            var ip = HttpContext.Connection.RemoteIpAddress;
            if (ip?.IsIPv4MappedToIPv6 == true) ip = ip.MapToIPv4();
            return ip?.ToString() ?? "unknown";
        }

        [HttpPost("ipaddresses")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddIpAddress([FromBody] CreateAllowedIpAddressRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IpAddress))
                return BadRequest("IP address is required.");

            var createdBy = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Name
                ?? User.Identity?.Name;
            var added = await _repository.AddAsync(request.IpAddress.Trim(), request.Description, createdBy);
            return added ? StatusCode(StatusCodes.Status201Created) : Conflict("IP address already exists.");
        }

        [HttpDelete("ipaddresses/{ipAddress}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteIpAddress(string ipAddress)
        {
            var deleted = await _repository.DeleteAsync(ipAddress);
            return deleted ? NoContent() : NotFound();
        }

        [HttpGet("settings")]
        [ProducesResponseType<SettingsDto>(StatusCodes.Status200OK)]
        public async Task<SettingsDto> GetSettings()
            => await _settingsRepository.GetAsync();

        [HttpPut("settings")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SaveSettings([FromBody] SettingsDto settings)
        {
            await _settingsRepository.SaveAsync(settings);
            return NoContent();
        }

        [HttpGet("paths")]
        [ProducesResponseType<IReadOnlyList<RestrictedPathDto>>(StatusCodes.Status200OK)]
        public async Task<IReadOnlyList<RestrictedPathDto>> GetRestrictedPaths()
            => await _pathsRepository.GetAllAsync();

        [HttpPost("paths")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddRestrictedPath([FromBody] CreateRestrictedPathRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Path))
                return BadRequest("Path is required.");

            var createdBy = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Name
                ?? User.Identity?.Name;
            var added = await _pathsRepository.AddAsync(request.Path.TrimEnd('/'), request.Description, createdBy);
            return added ? StatusCode(StatusCodes.Status201Created) : Conflict("Path already exists.");
        }

        [HttpDelete("paths/{*path}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRestrictedPath(string path)
        {
            var decoded = Uri.UnescapeDataString(path).TrimEnd('/');
            if (!decoded.StartsWith('/')) decoded = "/" + decoded;
            var deleted = await _pathsRepository.DeleteAsync(decoded);
            return deleted ? NoContent() : NotFound();
        }
    }
}
