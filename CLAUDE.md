# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

An Umbraco 17 + 18 / .NET 10 NuGet package (Razor Class Library) that restricts site access with a rule engine (`Ip`, `Path`, `UserGroup` conditions), managed from a Lit backoffice dashboard. Auto-registers via an Umbraco `IComposer`; no host app lives in this repo.

## Commands

The frontend must be built before `dotnet pack`, because Vite emits into `wwwroot/App_Plugins/...`, which ships in the package.

```bash
# Frontend (Lit + TS + Vite), from M1sterPl0w.Umbraco.AccessRestriction/Client
npm ci
npm run build          # tsc && vite build → ../wwwroot/App_Plugins/M1sterPl0w.Umbraco.AccessRestriction/
npm run watch
npm run generate-client     # regenerates src/api/* from a RUNNING Umbraco 18 site at https://localhost:5000 (hey-api)
npm run generate-client:17  # same, against an Umbraco 17 site (Swashbuckle URL)

# Backend, from repo root. -p:UmbracoMajor=17|18 selects the Umbraco major (default 18)
dotnet build -p:UmbracoMajor=17
dotnet test -p:UmbracoMajor=17
dotnet test --filter "FullyQualifiedName~IpConditionTests"           # one class
dotnet test --filter "FullyQualifiedName~IpConditionTests.MethodName" # one test
dotnet pack --configuration Release -p:UmbracoMajor=17 -p:Version=0.0.0-ci
```

CI (`.github/workflows/ci.yml`) runs a matrix over Umbraco 17 and 18: frontend build → `dotnet build` → `dotnet test` → pack dry-run. Publishing to NuGet happens on a GitHub release. The version comes from the tag (`v18.1.2` → `18.1.2`), and the tag's major selects `UmbracoMajor`.

**Versioning.** The package major equals the Umbraco major (`17.x` → Umbraco 17, `18.x` → Umbraco 18). `Directory.Build.props` defines `UmbracoMajor`, and `Directory.Packages.props` derives the Umbraco range `[$(UmbracoMajor).0.0,$(UmbracoNextMajor).0.0)` from it. `Directory.Build.targets` rejects unsupported majors, and fails pack when the version major doesn't match (`0.x` is exempt). Code that differs per major uses `#if UMBRACO_18`. Currently only the OpenAPI registration differs: Umbraco 17 uses Swashbuckle and Umbraco 18 uses `Microsoft.AspNetCore.OpenApi`.

Package versions are centrally managed in `Directory.Packages.props`. Don't put versions in the csproj files.

## Architecture

**Request pipeline.** `AccessRestrictionStartupFilter` (an `IStartupFilter`) inserts `AccessRestrictionMiddleware` at the very front of the pipeline. The middleware:
1. Loads settings and short-circuits if disabled.
2. Resolves client IP(s): the remote IP (IPv4-mapped IPv6 normalized), or the first entry of the configured header. If `ConsiderRemoteIp` is set, the remote IP is also included. The result is stored in `HttpContext.Items[Constants.ClientIpItemKey]`, and `IpCondition` reads it from there instead of from the connection.
3. Calls `IAccessRuleEngine.EvaluateAsync`. On deny, it redirects to the configured deny content node, or returns `DenyStatusCode`. It passes the request through if it already targets the deny page, which avoids redirect loops.

**Rule engine** (`RuleEngine/AccessRuleEngine.cs`). Rules are ordered by `SortOrder`; the first rule that matches decides the outcome (`Allow`/`Deny`). Rules without conditions are skipped. When no rules exist, or none match, access is allowed. Conditions are created from `ConditionDto.Type` strings with a `switch`. An unknown type becomes a never-matching condition and is logged.
- **Adding a condition type** means touching: a new `IAccessCondition` in `RuleEngine/Conditions/`, the `switch` in `AccessRuleEngine.CreateCondition`, `ValidConditionTypes` in the API controller, and the dashboard UI.

**Data and caching** (`Repository/`). Persistence uses Umbraco `IScopeProvider`/NPoco, with schemas in `Models/Data/`. There are three tables: settings (key/value rows), rules, and conditions (values stored as a JSON string array). Both repositories cache in `IMemoryCache` (5-minute sliding expiration) under `Constants.CacheKeys`, and every write must invalidate or refresh the cache.
- **Static rules** from `appsettings.json` (`AccessRestriction:Rules`, bound to `AccessRestrictionOptions`) are merged into `RuleRepository.GetAllAsync` with **negative IDs** and `CanDelete = false`. The UI treats them as read-only. Note that the engine re-sorts all rules by `SortOrder`, so static rules don't always run first.
- `AccessRestriction:IpHeader` in config overrides the DB value and sets `IsIpHeaderForced`, which locks the field in the UI.

**Migrations.** `AccessRestrictionMigrationPlan` runs on `UmbracoApplicationStartedNotification`. To change the schema, add a new migration class (derive from `AsyncMigrationBase`, because `MigrationBase` is gone in Umbraco 18) and chain `.To<NewMigration>("x.y")` onto the plan. Never edit existing steps.

**Backoffice API.** Controllers derive from `M1sterPl0wUmbracoAccessRestrictionApiControllerBase`: route `/umbraco/management/api/m1sterpl0wumbracoaccessrestriction/api/v1/...`, policy `SectionAccessContent`, and their own OpenAPI doc (`Constants.ApiName`). Operation IDs are set to the action name by `CustomOperationHandler` (Umbraco 17) or `AccessRestrictionOperationIdTransformer` (Umbraco 18). The two must stay in sync, because the operation IDs determine the generated TS method names.

**Frontend** (`Client/`). It is a single dashboard web component (`access-restriction-dashboard.element.ts`) registered in the Content section via `Client/public/umbraco-package.json`. Everything in `src/api/*.gen.ts` is generated by `npm run generate-client`, so don't hand-edit it; `src/hey-api.ts` configures the runtime client. `@umbraco*` imports are externals and are provided by the backoffice at runtime.

## Spec-driven changes (OpenSpec)

Behavior specs live in `openspec/specs/<capability>/spec.md`. Project constraints for agents live in `openspec/config.yaml`. To make a change, run `/opsx:propose`, then `/opsx:apply`, then `/opsx:archive`. Archiving merges the change's delta specs into `openspec/specs/`. Check specs with `npx @fission-ai/openspec validate --all --strict`.

## Gotchas

- Folder names and namespaces don't match. `Repository/*` uses namespace `...Services`, `Models/Dto|Settings/*` uses `...Models`, and `Models/Constant/*` uses `...Constant`. Follow the existing namespace of the folder you are editing.
- Tests (xUnit + Moq) build `HttpContext` via `Helpers/TestHttpContextBuilder.cs`.
- The README ends with a stale duplicate section that lists old tables (`AccessRestrictionIpAddresses`, `AccessRestrictionPaths`), and the csproj `<Description>` still describes the old IP-whitelist model.
