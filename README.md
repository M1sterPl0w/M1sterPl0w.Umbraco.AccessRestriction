# M1sterPl0w.Umbraco.AccessRestriction

An Umbraco package that controls access to your site through a flexible **rule engine**. Define access rules combining IP address, request path, and Umbraco user group conditions — managed from a dashboard in the Umbraco backoffice, with optional hardcoded static rules via `appsettings.json`.

---

## Features

- **Rule engine** — compose access rules from multiple conditions (`Ip`, `Path`, `UserGroup`)
- **Flexible IP matching** — exact address, CIDR range (e.g. `10.0.0.0/8`), or wildcard pattern (e.g. `192.168.1.*`)
- **AND / OR logic** — each rule can require ALL conditions to match or just ANY one
- **Allow / Deny outcomes** — rules can explicitly allow or deny access
- **Backoffice dashboard** — create and manage rules and their conditions without redeployment
- **Static rules via `appsettings.json`** — hardcode rules that are read-only in the UI
- **Umbraco user group condition** — restrict paths to specific backoffice user groups
- **Custom IP header** — support reverse proxies via `X-Forwarded-For` or `X-Real-IP`
- **IPv4-mapped IPv6** normalisation — `::ffff:x.x.x.x` is automatically mapped to `x.x.x.x`

---

## Requirements

- Umbraco 17+
- .NET 10
- Node LTS 20.17.0+ *(for building the frontend)*

---

## Installation

```
dotnet add package M1sterPl0w.Umbraco.AccessRestriction
```

The package auto-registers itself through Umbraco's composer pattern — no manual `Program.cs` changes needed.

---

## How it works

The middleware runs on every HTTP request:

1. **Disabled check** — if the restriction is disabled, allow all requests.
2. **No rules** — if no rules are configured, allow all requests.
3. **Rule evaluation** — rules are evaluated in sort order. The first rule whose conditions match determines the outcome (`Allow` or `Deny`).
4. **No match** — if no rule matches, the request is allowed through (default-allow).

### Condition types

| Type | Matches when… |
|---|---|
| `Ip` | The resolved client IP matches any entry in the allowlist (exact, CIDR, or wildcard — see below) |
| `Path` | The request path starts with any of the listed paths |
| `UserGroup` | The authenticated user belongs to any of the listed Umbraco user groups |

#### IP matching formats

Each value in an `Ip` condition can be:

| Format | Example | Matches |
|---|---|---|
| Exact IP | `192.168.1.1` | That specific address only |
| CIDR range | `192.168.1.0/24` | Any address in the subnet (`192.168.1.0` – `192.168.1.255`) |
| Wildcard `*` | `192.168.1.*` | Any address where `*` replaces one or more characters |
| Wildcard `?` | `10.0.0.?` | Any address where `?` replaces exactly one character |

> IPv6 CIDR ranges are also supported, e.g. `2001:db8::/32`.

> **Future-proof:** Combined with the rule's `Result` field, an `Ip` condition in an `Allow` rule acts as an IP allowlist; in a `Deny` rule it acts as an IP blocklist. A dedicated `IpBlocklist` condition type may be added in a future release.

### Rule logic

| `RequireAll` | Behaviour |
|---|---|
| `true` | ALL conditions in the rule must match (AND) |
| `false` | ANY condition in the rule must match (OR) |

---

## Configuration

### Enable / disable

Open the **Access Restriction** dashboard in the Umbraco backoffice and toggle **Enable access restriction**.

### Static rules via `appsettings.json`

Rules defined in configuration are read-only in the UI (marked **static**) and are always evaluated before database rules:

```json
"AccessRestriction": {
  "IpHeader": "X-Forwarded-For",
  "Rules": [
    {
      "Name": "Allow backoffice from office IPs",
      "Description": "Restrict /umbraco to known office IPs",
      "RequireAll": true,
      "Result": "Allow",
      "SortOrder": 0,
      "Conditions": [
        { "Type": "Path",  "Values": ["/umbraco"] },
        { "Type": "Ip",    "Values": ["1.2.3.4", "5.6.7.8", "192.168.1.*", "10.0.0.0/8"] }
      ]
    },
    {
      "Name": "Deny everyone else from backoffice",
      "RequireAll": false,
      "Result": "Deny",
      "SortOrder": 1,
      "Conditions": [
        { "Type": "Path", "Values": ["/umbraco"] }
      ]
    }
  ]
}
```

### IP header (reverse proxy / load balancer)

By default the middleware uses the connection's remote IP. To read the original client IP from a forwarding header:

**Via the dashboard** — fill in the **IP header** field (e.g. `X-Forwarded-For`).

**Via `appsettings.json`** — forces the value and locks the field in the UI:

```json
"AccessRestriction": {
  "IpHeader": "X-Forwarded-For"
}
```

> When using `X-Forwarded-For`, only the first (left-most) address is used.

---

## Database

Three tables are created automatically on first startup via Umbraco's migration system:

| Table | Purpose |
|---|---|
| `AccessRestrictionSettings` | Global settings (enabled flag, IP header) |
| `AccessRestrictionRules` | Access rules (name, logic, result, sort order) |
| `AccessRestrictionConditions` | Conditions belonging to each rule (type + JSON values) |

---

## Building the frontend

The backoffice dashboard is built with Lit + TypeScript using Vite.

```bash
cd M1sterPl0w.Umbraco.AccessRestriction/Client
npm install
npm run build
```

For development with file watching:

```bash
npm run watch
```

---

## License

MIT

| `AccessRestrictionIpAddresses` | Whitelisted IP addresses |
| `AccessRestrictionSettings` | Package settings (enabled flag) |
| `AccessRestrictionPaths` | Restricted paths |

---

## Building the frontend

The backoffice dashboard is built with Lit + TypeScript using Vite.

```bash
cd M1sterPl0w.Umbraco.AccessRestriction/Client
npm install
npm run build
```

For development with file watching:

```bash
npm run watch
```

> **Tip:** VS Code is the recommended editor — it has excellent TypeScript and Lit web component support.

---

## License

MIT
