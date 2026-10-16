# Design

## Context

See proposal.md for motivation. Requirements are in `specs/umbraco-compatibility/spec.md`.

- Umbraco 17 and Umbraco 18 both target `net10.0` (checked against the `Umbraco.Cms.Core` 18.2.1 nuspec). The target framework cannot separate them.
- Versions are centrally managed in `Directory.Packages.props`. Seven `Umbraco.Cms.*` entries are pinned to `[17.0.0,17.999)`.
- `publish.yml` derives `Version` from the release tag. `ci.yml` builds with `0.0.0-ci` on `main` only and does not run tests.
- A trial build against Umbraco 18.0.0 (2026-10-17) failed in two areas:
  - **Migrations:** `MigrationBase` no longer exists. `AsyncMigrationBase` (with `MigrateAsync()`) exists in both 17 and 18.
  - **OpenAPI:** Umbraco 18 replaced Swashbuckle with `Microsoft.AspNetCore.OpenApi`. `SwaggerGenOptions`, `BackOfficeSecurityRequirementsOperationFilterBase` and `OperationIdHandler` are gone. Umbraco 18 provides `builder.AddBackOfficeOpenApiDocument(name, b => ...)` with `WithBackOfficeAuthentication()` and `ConfigureOpenApiOptions`. Its built-in transformers (including `UmbracoOperationIdTransformer`) run before the configured callbacks. The document URL moves from `/umbraco/swagger/{doc}/swagger.json` to `/umbraco/openapi/{doc}.json`.
- `@umbraco-cms/backoffice@18` has `@hey-api/openapi-ts >=0.99.0 <1.0.0` as a peer dependency. The repo pins `^0.85`. The committed `src/api/*.gen.ts` is stale: it still describes the old IP-whitelist API, and the dashboard does not import it.
- Umbraco 18 pulls in transitive packages with NuGet audit warnings (`Microsoft.OpenApi` 2.0.0 high, `System.Security.Cryptography.Xml`/`Pkcs` 10.0.7, `MessagePack` 3.1.7).

## Goals / Non-Goals

**Goals:**
- Each branch's code targets one Umbraco major with no conditional compilation.
- The Umbraco major, dependency range and version guard come from one property per branch.

**Non-Goals:**
- Building both majors from one branch.
- Feature work on `release/v17`. It receives hotfixes only.
- Supporting Umbraco 16 or earlier, or pre-release Umbraco 19.
- Fixing the stale csproj `<Description>` or the stale README tail section.

## Decisions

### D1: Branch per Umbraco major
`main` is Umbraco 18 and `release/v17` is Umbraco 17. `release/v17` is cut from `origin/main` at `025e942` (the last Umbraco 17 commit, 2.0.2-era code plus OpenSpec), so it needs no code changes.
- Alternative considered: one branch with `-p:UmbracoMajor` and `#if UMBRACO_18`. It was implemented first and rejected by the owner, because `#if` spreads major-specific branches through the code.
- When Umbraco 19 arrives, `release/v18` is cut from `main` in the same way.

### D2: `UmbracoMajor` constant per branch
`Directory.Build.props` sets `UmbracoMajor` (18 on `main`, 17 on `release/v17`) and `UmbracoNextMajor`. `Directory.Packages.props` uses `[$(UmbracoMajor).0.0,$(UmbracoNextMajor).0.0)` for every `Umbraco.Cms.*` entry. The exclusive next-major bound replaces `.999)`.
- There are no `DefineConstants` and no supported-majors validation, because nothing on a branch varies by major.
- Keeping the property, rather than a literal range, gives the guard (D3) one value to compare against. It also makes the next branch cut a one-line change.

### D3: Version-major guard
`Directory.Build.targets` adds a target before `GenerateNuspec`. It fails when the `PackageVersion` major is not equal to `UmbracoMajor`, unless the major is `0` (CI dry-run). The error text names both values. This guard is what rejects a release tag published from the wrong branch, or with an unsupported major such as `v2.1.0`.

### D4: Workflows per branch
- **CI:** `ci.yml` triggers on push and PR to `main` and `release/v17`, without a matrix. It adds a `dotnet test` step.
- **Publish:** `publish.yml` keeps `VERSION=${GITHUB_REF_NAME#v}` and packs with it. The GitHub release's target branch decides the code, and D3 rejects a mismatch.
- Both branches carry the same workflow files.

### D5: Migrations use `AsyncMigrationBase`
On `main`, `InitialMigration` and `AddDenyResponseSettingsMigration` derive from `AsyncMigrationBase` and override `MigrateAsync()`. Steps, order, plan state keys and schema stay identical.
- This is the one sanctioned exception to "never edit existing migration steps", because the steps themselves are untouched. The project owner confirmed it (2026-10-17).
- New migrations on either branch derive from `AsyncMigrationBase`, so cherry-picks compile on both majors.
- `release/v17` keeps `MigrationBase`, which is valid on 17.

### D6: OpenAPI on `main` via `Microsoft.AspNetCore.OpenApi`
The composer calls `builder.AddBackOfficeOpenApiDocument(Constants.ApiName, b => b.WithTitle(...).WithBackOfficeAuthentication().ConfigureOpenApiOptions(o => o.AddOperationTransformer<AccessRestrictionOperationIdTransformer>()))`. The transformer sets `OperationId` to the action name for controllers in the package namespace. Configured transformers run after Umbraco's, so it overrides the default IDs.
- The Swashbuckle code (`SwaggerGenOptions`, the security filter, `CustomOperationHandler`) is removed from `main`. It stays on `release/v17`.
- Operation IDs on 18 equal those on 17, so generated TS method names match across branches.

### D7: Frontend on `main` uses Umbraco 18 types
Bump `@umbraco-cms/backoffice` to `^18` and `@hey-api/openapi-ts` to `^0.99`, adjusting `scripts/generate-openapi.js` and `src/hey-api.ts` to the 0.99 API where needed. Regenerate `src/api/*` from an Umbraco 18 site. `generate-client` uses the 18 URL. `release/v17` keeps its current frontend and Swashbuckle URL.

## Risks / Trade-offs

- [Branches drift; a fix reaches only one major] → The hotfix flow is in the spec. Each fix PR states whether it needs a cherry-pick. `release/v17` gets CI on every PR.
- [Every shared fix needs two releases] → Accepted cost of the version alignment.
- [Cherry-picks conflict where `main` diverged (migrations base class, OpenAPI)] → Those files rarely change. New migrations use `AsyncMigrationBase` (D5).
- [hey-api 0.85 → 0.99 changes generated code or plugin config] → Task 3.1 builds the frontend and checks that the dashboard still loads on an 18 site. The dashboard does not import `src/api`, which limits the impact.
- [Changing the base class of an existing migration alters already-applied state] → Plan keys and steps are unchanged. An upgrade test from `2.0.2` was done on 17. Task 2.6 repeats it on 18.
- [Transitive vulnerable packages on Umbraco 18] → Owned by Umbraco and not pinned by this package. Note in the release notes if still present.
- [Users on `2.0.2` don't notice the jump to `17.0.0`] → README version table. The release notes for `17.0.0` state that it continues 2.x.

## Migration Plan

1. Push `release/v17` from `025e942`, then merge its setup PR (range, guard, workflows, README).
2. Merge the `main` PR (Umbraco 18).
3. Publish `v17.0.0` targeting `release/v17` and `v18.0.0` targeting `main`.
4. Rollback: unlist the faulty package on NuGet. `2.0.2` remains available for Umbraco 17.
