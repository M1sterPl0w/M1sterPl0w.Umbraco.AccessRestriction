# rule-management Specification

## Purpose
Manage settings, rules and conditions through the backoffice Management API, and merge read-only static rules from configuration.

## Requirements

### Requirement: Backoffice API access
The API SHALL be served under `/umbraco/management/api/m1sterpl0wumbracoaccessrestriction/api/v1/` and MUST require the `SectionAccessContent` policy.

#### Scenario: Unauthorized caller
- **WHEN** a caller without Content section access calls the API
- **THEN** the request is rejected

### Requirement: Settings validation
Saving settings MUST reject a `DenyStatusCode` outside 100-599 with 400.

#### Scenario: Invalid status code
- **WHEN** settings are saved with `DenyStatusCode` 700
- **THEN** the API returns 400

### Requirement: Forced IP header
When `AccessRestriction:IpHeader` is set in configuration, it SHALL override the stored value, `IsIpHeaderForced` SHALL be true, and saving settings SHALL NOT overwrite the stored IP header.

#### Scenario: Config forces header
- **WHEN** configuration sets `IpHeader` to `CF-Connecting-IP`
- **THEN** settings return `CF-Connecting-IP` with `IsIpHeaderForced = true`

### Requirement: Rule validation
Creating or updating a rule MUST require a non-empty `Name` and a `Result` of exactly `Allow` or `Deny`; otherwise the API returns 400.

#### Scenario: Missing name
- **WHEN** a rule is created with an empty name
- **THEN** the API returns 400

### Requirement: Condition validation
Adding a condition MUST require a `Type` of `Ip`, `Path` or `UserGroup` (case-sensitive) and at least one value; otherwise the API returns 400.

#### Scenario: Invalid type
- **WHEN** a condition is added with type `ip`
- **THEN** the API returns 400

### Requirement: Delete cascades
Deleting a rule SHALL delete its conditions. Deleting a missing rule or condition SHALL return 404.

#### Scenario: Delete rule with conditions
- **WHEN** a rule with two conditions is deleted
- **THEN** the rule and both conditions are removed

### Requirement: Static rules
Rules from `AccessRestriction:Rules` in configuration SHALL be returned with the database rules, with negative IDs and `CanDelete = false`, and SHALL NOT be stored in the database.

#### Scenario: Static rule listed
- **WHEN** configuration defines one static rule
- **THEN** the rule list contains it with `Id = -1` and `CanDelete = false`

#### Scenario: Delete static rule
- **WHEN** a delete is requested for rule ID `-1`
- **THEN** the API returns 404

### Requirement: Cache consistency
Rules and settings SHALL be cached in `IMemoryCache` with a 5-minute sliding expiration, and every write MUST invalidate or refresh the cache.

#### Scenario: Rule change visible immediately
- **WHEN** a rule is created
- **THEN** the next rule evaluation includes the new rule
