# access-conditions Specification

## Purpose
Define how each condition type (`Ip`, `Path`, `UserGroup`) matches a request.

## Requirements

### Requirement: Ip condition
The `Ip` condition SHALL match when any resolved client IP matches any configured value, using exact match (case-insensitive), CIDR ranges, or `*`/`?` wildcards. It SHALL read client IPs from `HttpContext.Items[Constants.ClientIpItemKey]` and fall back to the connection remote IP only when that item is absent.

#### Scenario: Exact IP
- **WHEN** the client IP is `10.0.0.5` and the values contain `10.0.0.5`
- **THEN** the condition matches

#### Scenario: CIDR range
- **WHEN** the client IP is `192.168.1.20` and the values contain `192.168.1.0/24`
- **THEN** the condition matches

#### Scenario: Wildcard
- **WHEN** the client IP is `10.1.2.3` and the values contain `10.1.*`
- **THEN** the condition matches

#### Scenario: Empty resolved IP list
- **WHEN** the middleware stored an empty IP list
- **THEN** the condition does not match

### Requirement: Path condition
The `Path` condition SHALL match when the request path equals a configured path or starts with it followed by `/`, case-insensitive, ignoring a trailing `/` on the configured value.

#### Scenario: Sub-path
- **WHEN** the request path is `/members/profile` and the values contain `/members/`
- **THEN** the condition matches

#### Scenario: Prefix without separator
- **WHEN** the request path is `/membership` and the values contain `/members`
- **THEN** the condition does not match

### Requirement: UserGroup condition
The `UserGroup` condition SHALL match only for an authenticated user who is in any configured role.

#### Scenario: Anonymous user
- **WHEN** the request user is not authenticated
- **THEN** the condition does not match

#### Scenario: Authenticated user in group
- **WHEN** the authenticated user is in role `editor` and the values contain `editor`
- **THEN** the condition matches
