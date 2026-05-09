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
        private readonly IRuleRepository _ruleRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        private static readonly HashSet<string> ValidConditionTypes =
            new(StringComparer.Ordinal) { "Ip", "Path", "UserGroup" };

        public M1sterPl0wUmbracoAccessRestrictionApiController(
            IRuleRepository ruleRepository,
            ISettingsRepository settingsRepository,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        {
            _ruleRepository = ruleRepository;
            _settingsRepository = settingsRepository;
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        [HttpGet("ping")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public string Ping() => "Pong";

        // ── Settings ─────────────────────────────────────────────────────────────

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

        [HttpGet("myip")]
        [ProducesResponseType<string>(StatusCodes.Status200OK)]
        public async Task<string> GetMyIp()
        {
            var settings = await _settingsRepository.GetAsync();
            if (!string.IsNullOrWhiteSpace(settings.IpHeader))
            {
                var headerValue = HttpContext.Request.Headers[settings.IpHeader].FirstOrDefault();
                var ip = headerValue?.Split(',')[0].Trim();
                return string.IsNullOrEmpty(ip) ? "unknown" : ip;
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp?.IsIPv4MappedToIPv6 == true) remoteIp = remoteIp.MapToIPv4();
            return remoteIp?.ToString() ?? "unknown";
        }

        // ── Rules ─────────────────────────────────────────────────────────────────

        [HttpGet("rules")]
        [ProducesResponseType<IReadOnlyList<AccessRuleDto>>(StatusCodes.Status200OK)]
        public async Task<IReadOnlyList<AccessRuleDto>> GetRules()
            => await _ruleRepository.GetAllAsync();

        [HttpPost("rules")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateRule([FromBody] CreateRuleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required.");
            if (request.Result != "Allow" && request.Result != "Deny")
                return BadRequest("Result must be 'Allow' or 'Deny'.");

            var createdBy = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Name
                ?? User.Identity?.Name;
            var id = await _ruleRepository.CreateRuleAsync(request, createdBy);
            return StatusCode(StatusCodes.Status201Created, new { id });
        }

        [HttpPut("rules/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateRule(int id, [FromBody] UpdateRuleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required.");
            if (request.Result != "Allow" && request.Result != "Deny")
                return BadRequest("Result must be 'Allow' or 'Deny'.");

            var updated = await _ruleRepository.UpdateRuleAsync(id, request);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("rules/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteRule(int id)
        {
            var deleted = await _ruleRepository.DeleteRuleAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        // ── Conditions ────────────────────────────────────────────────────────────

        [HttpPost("rules/{ruleId:int}/conditions")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AddCondition(int ruleId, [FromBody] CreateConditionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Type))
                return BadRequest("Type is required.");
            if (!ValidConditionTypes.Contains(request.Type))
                return BadRequest($"Type must be one of: {string.Join(", ", ValidConditionTypes)}.");
            if (request.Values.Count == 0)
                return BadRequest("At least one value is required.");

            var id = await _ruleRepository.AddConditionAsync(ruleId, request);
            return StatusCode(StatusCodes.Status201Created, new { id });
        }

        [HttpDelete("conditions/{conditionId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCondition(int conditionId)
        {
            var deleted = await _ruleRepository.DeleteConditionAsync(conditionId);
            return deleted ? NoContent() : NotFound();
        }
    }
}

