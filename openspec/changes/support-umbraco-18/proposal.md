# Proposal

## Why

The package only installs on Umbraco 17 (`Umbraco.Cms.*` pinned to `[17.0.0,17.999)`), while Umbraco 18 is released (18.0.0 through 18.2.1 on NuGet). Sites that upgrade to Umbraco 18 cannot use the package. The package version (currently `2.0.2`) also gives users no hint which Umbraco major it works with.

## What Changes

- `main` targets Umbraco 18 only and releases `18.x.y`.
- A new support branch, `release/v17`, is cut from the current Umbraco 17 code on `main` (`025e942`). It releases `17.x.y` and receives Umbraco 17 hotfixes. No conditional compilation and no CI matrix: each branch builds exactly one Umbraco major.
- The package major version equals the Umbraco major: `17.x.y` depends on Umbraco 17 only, and `18.x.y` depends on Umbraco 18 only. Minor and patch numbers belong to this package and do not track Umbraco minors.
- **BREAKING** (versioning only): the next releases jump from `2.0.2` to `17.0.0` (from `release/v17`) and `18.0.0` (from `main`). Runtime behavior and the public API do not change.
- Pack fails when the version major does not equal the branch's Umbraco major. A wrongly targeted release tag therefore fails before publishing.
- Hotfix flow: fixes land on `main` and are cherry-picked to `release/v17` when they apply to Umbraco 17. Fixes that only affect 17 land on `release/v17` directly.
- CI and release workflows run on both `main` and `release/v17`, each verifying its own major.
- The README documents the package-to-Umbraco version mapping and the branch policy.

## Capabilities

### New Capabilities
- `umbraco-compatibility`: which Umbraco versions a package release supports, how the package version relates to the Umbraco major, which branch produces each major, and how hotfixes flow between branches.

### Modified Capabilities

None. Request handling, rule engine, conditions and rule management keep their behavior on both majors.

## Impact

- **`main`:**
  - `Directory.Packages.props` takes its Umbraco range from a branch-fixed `UmbracoMajor` (18).
  - A version-major guard at pack time.
  - Migrations move to `AsyncMigrationBase`, because Umbraco 18 removed `MigrationBase`.
  - OpenAPI registration moves from Swashbuckle to `Microsoft.AspNetCore.OpenApi`, because Umbraco 18 dropped Swashbuckle.
  - `generate-client` points at the Umbraco 18 OpenAPI URL.
  - `@umbraco-cms/backoffice` goes to `^18`, with `@hey-api/openapi-ts` `^0.99` (its peer dependency), and the stale `src/api` client is regenerated.
- **`release/v17`:**
  - Umbraco range `[17.0.0,18.0.0)` via `UmbracoMajor` 17.
  - The same version guard.
  - CI and publish triggers for that branch.
  - README version table. No code changes.
- **Both branches:** `.github/workflows/ci.yml` (adds `dotnet test`), `.github/workflows/publish.yml`, `README.md`, `CLAUDE.md`. `umbraco-marketplace.json` stays at minimum Umbraco 17.
- **Out of scope:** the stale csproj `<Description>` and the stale README tail section.
