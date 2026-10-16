# rule-engine Specification

## Purpose
Decide whether a request is allowed by evaluating the configured access rules in order.

## Requirements

### Requirement: Default allow
The engine SHALL allow access when no rules exist or when no rule matches.

#### Scenario: No rules configured
- **WHEN** the rule repository returns no rules
- **THEN** the request is allowed

#### Scenario: No rule matches
- **WHEN** rules exist but none of them match the request
- **THEN** the request is allowed

### Requirement: First match decides
The engine SHALL evaluate rules in ascending `SortOrder` across both database and static rules, and the first matching rule's `Result` (`Allow` or `Deny`) SHALL decide the outcome.

#### Scenario: Deny rule sorted first
- **WHEN** a matching `Deny` rule has a lower `SortOrder` than a matching `Allow` rule
- **THEN** the request is denied

### Requirement: Condition combination
A rule with `RequireAll` set SHALL match only when all of its conditions match; otherwise it SHALL match when any condition matches.

#### Scenario: RequireAll with one failing condition
- **WHEN** a rule has `RequireAll = true` and one of its conditions does not match
- **THEN** the rule does not match and evaluation continues with the next rule

#### Scenario: Any-match
- **WHEN** a rule has `RequireAll = false` and at least one condition matches
- **THEN** the rule matches

### Requirement: Rules without conditions are skipped
The engine MUST skip a rule that has no conditions.

#### Scenario: Empty rule
- **WHEN** a rule has zero conditions
- **THEN** the rule is ignored and evaluation continues

### Requirement: Unknown condition types never match
The engine SHALL treat a condition with an unrecognized `Type` as never matching and SHALL log a warning.

#### Scenario: Unknown type
- **WHEN** a condition has a `Type` other than `Ip`, `Path` or `UserGroup`
- **THEN** that condition does not match and a warning is logged
