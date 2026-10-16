# request-middleware Specification

## Purpose
Enforce access restriction on every request, at the front of the ASP.NET Core pipeline.

## Requirements

### Requirement: Disabled bypass
When the `Enabled` setting is false, the middleware SHALL pass every request through without evaluating rules.

#### Scenario: Restriction disabled
- **WHEN** `Enabled` is false
- **THEN** the request continues to the next middleware

### Requirement: Client IP resolution
The middleware SHALL resolve client IPs and store them in `HttpContext.Items[Constants.ClientIpItemKey]` before rule evaluation. IPv4-mapped IPv6 remote addresses MUST be normalized to IPv4.

#### Scenario: No IP header configured
- **WHEN** `IpHeader` is empty
- **THEN** the stored list contains only the remote IP

#### Scenario: IP header configured
- **WHEN** `IpHeader` is `X-Forwarded-For` and its value is `1.2.3.4, 5.6.7.8`
- **THEN** the stored list contains `1.2.3.4`

#### Scenario: Header plus remote IP
- **WHEN** `IpHeader` is set and `ConsiderRemoteIp` is true
- **THEN** the stored list contains the first header IP and the remote IP

#### Scenario: Header missing
- **WHEN** `IpHeader` is set, the header is absent, and `ConsiderRemoteIp` is false
- **THEN** the stored list is empty

### Requirement: Deny response
On deny, the middleware SHALL redirect to the URL of `DenyContentNodeKey` when it resolves to a URL; otherwise it SHALL respond with `DenyStatusCode` and body `Access denied.`.

#### Scenario: Deny page configured
- **WHEN** the request is denied and the deny content node resolves to `/no-access`
- **THEN** the response redirects to `/no-access`

#### Scenario: No deny page
- **WHEN** the request is denied and no deny content node resolves
- **THEN** the response status is `DenyStatusCode`

### Requirement: Deny page loop guard
The middleware MUST pass through a denied request whose path already equals the deny page path, ignoring query string, trailing `/` and case.

#### Scenario: Request targets deny page
- **WHEN** the request path is `/no-access/` and the deny page URL is `/no-access`
- **THEN** the request continues without a redirect
