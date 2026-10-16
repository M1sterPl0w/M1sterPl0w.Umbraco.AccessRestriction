# Design

## Context

See proposal.md for motivation. Requirements are in `specs/umbraco-compatibility/spec.md`.

- Umbraco 17 and Umbraco 18 both target `net10.0` (checked against the `Umbraco.Cms.Core` 18.2.1 nuspec). Multi-targeting by TFM cannot separate them, so one build produces one Umbraco major.
- Versions are centrally managed in `Directory.Packages.props`, with `CentralPackageFloatingVersionsEnabled`. Seven `Umbraco.Cms.*` entries are pinned to `[17.0.0,17.999)`.
- `publish.yml` derives `Version` from the release tag and packs once. `ci.yml` builds once with `0.0.0-ci` and does not run tests.
- Frontend: `@umbraco-cms/backoffice` is a dev dependency (`^17.3.4`) used for types only. At runtime the backoffice provides it as an external. The built JS does not bundle it.
- Trial build (2026-10-17, scratch copy): the baseline builds on Umbraco 17.0.0 and all 66 tests pass. Against Umbraco 18.0.0 the build fails with 5 errors in two areas:
  - **Migrations:** `MigrationBase` no longer exists in 18. `AsyncMigrationBase` (with `MigrateAsync()`) exists in both 17 and 18. In 17, `MigrationBase` derives from it.
  - **OpenAPI:** Umbraco 18 replaced Swashbuckle with `Microsoft.AspNetCore.OpenApi`. `SwaggerGenOptions`, `BackOfficeSecurityRequirementsOperationFilterBase`, `OperationIdHandler` and `IOperationIdHandler` are gone. Umbraco 18 uses `builder.AddBackOfficeOpenApiDocument(name, b => b.WithTitle(..).WithBackOfficeAuthentication())` plus `IOpenApiOperationTransformer`. Umbraco 17 does not have that API, so the two majors share no OpenAPI registration code. The document URL also changes from `/umbraco/swagger/{doc}/swagger.json` (17) to `/umbraco/openapi/{doc}.json` (18).
- Umbraco 18 pulls in transitive packages with NuGet audit warnings (`Microsoft.OpenApi` 2.0.0 high, `System.Security.Cryptography.Xml`/`Pkcs` 10.0.7, `MessagePack` 3.1.7).

## Goals / Non-Goals

**Goals:**
- One MSBuild property selects the Umbraco major. Everything else (dependency ranges, version guard, compile constants) derives from it.
- Release and CI select the major explicitly. No hand edits are needed per release.

**Non-Goals:**
- Separate branches per Umbraco major.
- Supporting Umbraco 16 or earlier, or pre-release Umbraco 19.
- Shipping two different frontend bundles.
- Fixing the stale csproj `<Description>` or the stale README tail section.

## Decisions

### D1: `UmbracoMajor` MSBuild property in a new `Directory.Build.props`
Define `UmbracoMajor` with a default of `18` (the latest stable) when it is not already set. Compute `UmbracoNextMajor` with `$([MSBuild]::Add($(UmbracoMajor), 1))`. In `Directory.Packages.props`, every `Umbraco.Cms.*` entry uses `Version="[$(UmbracoMajor).0.0,$(UmbracoNextMajor).0.0)"`.
- The `.999)` upper bound is replaced by an exclusive next-major bound. It matches the spec exactly and does not miss a hypothetical `N.999.x`.
- Validate `UmbracoMajor` is `17` or `18` and error otherwise. This blocks typos such as `-p:UmbracoMajor=81`.
- Alternative considered: two props files swapped by the workflow. Rejected because it is more files and drift-prone.

### D2: Version-major guard as an MSBuild target
Add a target before `GenerateNuspec` in the package csproj (or `Directory.Build.targets`). It fails when `Version` major does not equal `UmbracoMajor`, unless `Version` starts with `0.` (the CI dry-run). Error text names both values.

### D3: Release derives the major from the tag
`publish.yml` parses `VERSION=${GITHUB_REF_NAME#v}` and then `MAJOR=${VERSION%%.*}`. It passes `-p:UmbracoMajor=$MAJOR` to restore, build and pack. A major outside {17, 18} fails through the D1 validation before anything is pushed. Releases for each major are separate tags (`v17.0.0`, `v18.0.0`) cut from the same commit or from later commits on `main`.

### D4: CI matrix
`ci.yml` gets `strategy.matrix.umbraco: [17, 18]`. Each leg runs `dotnet restore/build/test/pack -p:UmbracoMajor=${{ matrix.umbraco }}`. The frontend build runs in each leg, which is cheap. Adding `dotnet test` closes the gap where CI never ran tests.

### D5: Frontend built once against Umbraco 18 types
Bump `@umbraco-cms/backoffice` to `^18` in `Client/package.json`. The dashboard uses stable backoffice APIs (element base, notification and auth context, `umbraco-package.json` dashboard manifest). Those are expected to be identical in 17 and 18. The runtime library is external, so one bundle serves both.
- Alternative considered: install the matching backoffice version per CI leg. Rejected because it rewrites the lockfile in CI and doubles frontend work for type-only differences.
- If 18 types break the Umbraco 17 runtime, fall back to keeping `^17` types and verify manually on 18.

### D6: Conditional compilation only where no shared API exists
Expose `UMBRACO_$(UmbracoMajor)` (for example `UMBRACO_18`) through `DefineConstants`. Use `#if` only where no API works on both majors, and keep the divergent code in the smallest possible scope.

### D6a: Migrations move to `AsyncMigrationBase`
Change `InitialMigration` and `AddDenyResponseSettingsMigration` to derive from `AsyncMigrationBase` and override `MigrateAsync()`. This works on both majors without `#if`. Only the base class and method signature change. The migration steps, their order, the plan state keys and the resulting schema stay identical. This is the one sanctioned exception to "never edit existing migration steps", because the steps themselves are untouched. The project owner confirmed this choice (2026-10-17) over per-major `#if` around the migration classes.

### D6b: OpenAPI registration split per major
Select the OpenAPI registration in the composer with `#if UMBRACO_18`. The Umbraco 18 transformer lives in its own file, compiled only for 18:
- 17: the existing Swashbuckle code (`SwaggerGenOptions`, the security filter, and the `OperationIdHandler` subclass), unchanged.
- 18: `builder.AddBackOfficeOpenApiDocument(Constants.ApiName, b => b.WithTitle(...).WithBackOfficeAuthentication())`, plus an `IOpenApiOperationTransformer` registered on that document. The transformer sets `OperationId` to the action name for controllers in the package namespace.

Generated TS method names depend on operation IDs, so both majors must emit the same IDs. `generate-client` then works against either major. Its URL argument must match the host's major.

### D7: Test project follows the same switch
The test project gets Umbraco transitively through the project reference. `Directory.Build.props` applies to it as well, so `dotnet test -p:UmbracoMajor=17|18` exercises both.

## Risks / Trade-offs

- [Confirmed Umbraco 18 breaks in migrations and OpenAPI] → D6a and D6b. No other compile errors surfaced, but errors in the first pass can hide later ones, so task 2 rebuilds after each fix.
- [Operation IDs differ between majors, so the generated client breaks on one major] → Task 2.3 compares the operation IDs in the 17 and 18 OpenAPI documents.
- [Changing the base class of an existing migration alters already-applied state] → Plan step keys and steps are unchanged. Upgrade test on a DB migrated by `2.0.2` (task 5.3).
- [Transitive vulnerable packages on Umbraco 18] → Owned by Umbraco and not pinned by this package. Note in the README if still present at release.
- [Umbraco 18 backoffice changes break the dashboard at runtime] → Manual smoke test on a 17 site and an 18 site before the first release (task 5).
- [Users on `2.0.2` don't notice the jump to `17.0.0`] → README version table. The release notes for `17.0.0` state that it continues 2.x for Umbraco 17.
- [Fixes need two releases (17.x.y and 18.x.y)] → Accepted cost of the version alignment. Both come from the same commit.
- [`dotnet` default without `-p:UmbracoMajor` silently builds 18] → Intended for local dev. CI and release always pass it explicitly.

## Migration Plan

1. Merge the change. CI is green for both majors.
2. Publish release `v17.0.0` and then `v18.0.0` from the same commit.
3. Rollback: unlist the faulty package on NuGet. `2.0.2` remains available for Umbraco 17.
