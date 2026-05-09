using M1sterPl0w.Umbraco.AccessRestriction.Models;
using M1sterPl0w.Umbraco.AccessRestriction.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace M1sterPl0w.Umbraco.AccessRestriction.RuleEngine
{
    public class AccessRuleEngine : IAccessRuleEngine
    {
        private readonly IRuleRepository _ruleRepository;
        private readonly ILogger<AccessRuleEngine> _logger;

        public AccessRuleEngine(IRuleRepository ruleRepository, ILogger<AccessRuleEngine> logger)
        {
            _ruleRepository = ruleRepository;
            _logger = logger;
        }

        public async Task<bool> EvaluateAsync(HttpContext context)
        {
            var rules = await _ruleRepository.GetAllAsync();

            if (rules.Count == 0)
                return true; // No rules configured → allow everything

            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                if (rule.Conditions.Count == 0)
                    continue; // Skip empty rules

                var conditions = rule.Conditions
                    .Select(CreateCondition)
                    .ToList();

                var matches = rule.RequireAll
                    ? conditions.All(c => c.IsMatch(context))
                    : conditions.Any(c => c.IsMatch(context));

                if (matches)
                    return rule.Result == "Allow";
            }

            return true; // No rule matched → default allow
        }

        private IAccessCondition CreateCondition(ConditionDto dto)
        {
            return dto.Type switch
            {
                "Ip"        => new IpCondition        { AllowedIps = dto.Values },
                "Path"      => new PathCondition      { Paths      = dto.Values },
                "UserGroup" => new UserGroupCondition { Groups     = dto.Values },
                _           => LogUnknownAndDeny(dto.Type)
            };
        }

        private IAccessCondition LogUnknownAndDeny(string type)
        {
            _logger.LogWarning("Unknown condition type '{Type}' skipped in rule engine.", type);
            return new AlwaysDenyCondition();
        }
    }

    /// <summary>Sentinel condition that never matches — used when a condition type is unrecognised.</summary>
    internal sealed class AlwaysDenyCondition : IAccessCondition
    {
        public bool IsMatch(HttpContext context) => false;
    }
}
