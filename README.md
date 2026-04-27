# M1sterPl0w.Umbraco.AccessRestriction

An Umbraco package that restricts access to your site (or specific paths) by IP address whitelist. Manage whitelisted IPs and restricted paths from a dashboard in the Umbraco backoffice, with optional hardcoded entries via `appsettings.json`.

---

## Features

- **IP whitelisting** — only allow requests from specific IP addresses
- **Restricted paths** — limit enforcement to specific paths (e.g. `/umbraco`); when no paths are configured, the entire site is restricted
- **Backoffice dashboard** — manage IPs and paths through a built-in Umbraco dashboard
- **Static entries via `appsettings.json`** — hardcode IPs and paths that cannot be deleted through the UI
- **Force-enabled** — if any IPs are configured in `appsettings.json`, the restriction is always active and cannot be disabled from the UI
- **IPv4-mapped IPv6** support — `::ffff:x.x.x.x` addresses are automatically normalised

---

## Requirements

- Umbraco 13+
- .NET 10
- Node LTS 20.17.0+ *(for building the frontend)*

---

## Installation

Install via NuGet:

```
dotnet add package M1sterPl0w.Umbraco.AccessRestriction
```

The package auto-registers itself through Umbraco's composer pattern — no manual `Program.cs` changes needed.

---

## Configuration

### Enable / disable

Open the **Access Restriction** dashboard in the Umbraco backoffice and toggle **Enable IP whitelisting**.

### `appsettings.json`

Add the following section to your `appsettings.json` to hardcode IPs and/or paths:

```json
"AccessRestriction": {
  "IpAddresses": [
    { "IpAddress": "1.2.3.4", "Description": "Office" },
    { "IpAddress": "5.6.7.8", "Description": "VPN" }
  ],
  "Paths": [
    { "Path": "/umbraco", "Description": "Backoffice" }
  ]
}
```

- Entries from `appsettings.json` appear in the dashboard marked **static** and cannot be deleted.
- If any `IpAddresses` are present, the restriction is **force-enabled** and the toggle in the dashboard is locked.

---

## How it works

The middleware runs on every request and applies the following logic:

1. **Disabled check** — if the restriction is disabled *and* not force-enabled by config, the request is allowed through.
2. **Path check** — if restricted paths are configured, only requests matching those paths are subject to the IP check. Requests to other paths are allowed through.
3. **No paths configured** — all paths are restricted.
4. **No IPs configured** — all requests are allowed (restriction is inactive).
5. **IP check** — if the client IP is in the whitelist, the request is allowed. Otherwise a `403 Forbidden` is returned.

---

## Database

Three tables are created automatically on first startup via Umbraco's migration system:

| Table | Purpose |
|---|---|
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
