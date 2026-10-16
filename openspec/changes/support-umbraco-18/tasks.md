# Tasks

Groups 1–4 and 6 run on the `main` PR branch (`feature/support-umbraco-18`). Group 5 runs on `release/v17`.

## 1. Build configuration on main (Umbraco 18 only)

- [x] 1.1 Reduce `Directory.Build.props` to fixed `UmbracoMajor` 18 and `UmbracoNextMajor`, removing the `-p:` override default, `SupportedUmbracoMajors` and the `UMBRACO_*` `DefineConstants`. Remove the `ValidateUmbracoMajor` target from `Directory.Build.targets`. Verify that `dotnet restore` resolves `Umbraco.Cms.Core` 18.x and that `grep -rn "UMBRACO_1\|SupportedUmbracoMajors"` finds nothing.
- [x] 1.2 Drive every `Umbraco.Cms.*` entry in `Directory.Packages.props` from `[$(UmbracoMajor).0.0,$(UmbracoNextMajor).0.0)`. Verify that `obj/project.assets.json` resolves `Umbraco.Cms.Core` 18.x.
- [x] 1.3 Add the version-major guard target (design D3). Verify that `dotnet pack -p:Version=17.1.0` fails with an error naming both majors, and that `-p:Version=18.0.0` and `-p:Version=0.0.0-ci` succeed.

## 2. Umbraco 18 code on main (no conditional compilation)

- [x] 2.1 Switch `InitialMigration` and `AddDenyResponseSettingsMigration` to `AsyncMigrationBase`/`MigrateAsync()` with identical steps (design D5). Verify that the project builds and `AccessRestrictionMigrationPlan` is unchanged.
- [x] 2.2 Remove all `#if UMBRACO_18` / `#if !UMBRACO_18` blocks and the Swashbuckle code (`SwaggerGenOptions`, the security filter, `CustomOperationHandler`) from the composer. Keep only the `AddBackOfficeOpenApiDocument` registration (design D6). Verify that `dotnet build` succeeds with no compiler warnings and that `grep -rn "#if UMBRACO" --include=*.cs` finds nothing.
- [x] 2.3 Remove the `#if UMBRACO_18` wrapper from `AccessRestrictionOperationIdTransformer.cs` and its tests. Verify that `dotnet test` passes, including the 3 transformer tests.
- [x] 2.4 Set `generate-client` to the Umbraco 18 URL only and remove `generate-client:17` from `Client/package.json`. Verify that `npm run` lists a single `generate-client` script.
- [x] 2.5 Inspect `dotnet pack -p:Version=18.0.0` output. Verify that the nuspec lists `Umbraco.Cms.*` dependencies as `[18.0.0, 19.0.0)`.
- [ ] 2.6 Smoke test on a minimal Umbraco 18 site with the locally packed `18.0.0`:
  - A static deny rule returns 403 and the allow rule passes.
  - The dashboard renders in the backoffice.
  - `/umbraco/openapi/m1sterpl0wumbracoaccessrestriction.json` operation IDs equal the Umbraco 17 set (`AddCondition, CreateRule, DeleteCondition, DeleteRule, GetMyIp, GetRules, GetSettings, Ping, SaveSettings, UpdateRule`).
  - Upgrading a DB migrated by `2.0.2` re-runs no migration.

## 3. Frontend on main

- [x] 3.1 Bump `@umbraco-cms/backoffice` to `^18` and `@hey-api/openapi-ts` to `^0.99` in `Client/package.json`, refresh `package-lock.json`, and adapt `scripts/generate-openapi.js` / `src/hey-api.ts` to the 0.99 API if needed. Verify that `npm install` reports no peer-dependency errors.
- [x] 3.2 Regenerate `src/api/*` with `npm run generate-client` against a running Umbraco 18 site (do not hand-edit the generated files). Verify that the generated SDK contains the 10 operations from 2.6 and no IP-whitelist operations, and that `npm run build` in `Client/` succeeds.

## 4. CI and release workflows on main

- [x] 4.1 Update `.github/workflows/ci.yml`: remove the matrix and the `-p:UmbracoMajor` arguments, trigger on push and PR to `main` and `release/v17`, and keep the `dotnet test` step. Verify that the YAML parses and that the restore/build/test/pack commands run locally.
- [x] 4.2 Update `.github/workflows/publish.yml`: remove the `UMBRACO_MAJOR` derivation and the `-p:UmbracoMajor` arguments, and pack with the tag version only. Verify locally on `main` that pack succeeds with `-p:Version=18.0.0` and fails for `17.0.1` and `2.1.0`.

## 5. Umbraco 17 support branch (release/v17)

- [x] 5.1 Create `release/v17` from `origin/main` at `025e942` and push it. Verify with `git log -1 origin/release/v17` and with `dotnet build` resolving Umbraco 17.x.
- [x] 5.2 On a PR branch into `release/v17`:
  - Add `Directory.Build.props` with `UmbracoMajor` 17 and `UmbracoNextMajor`, plus `Directory.Build.targets` with the version guard.
  - Switch `Directory.Packages.props` to the `UmbracoMajor`-driven range.
  - Apply the same `ci.yml`/`publish.yml` changes as group 4.

  Verify that the nuspec lists `[17.0.0, 18.0.0)`, that `dotnet pack -p:Version=18.0.0` fails, and that `dotnet test` passes.
- [x] 5.3 On the same PR branch, add the README version table, branch policy and hotfix flow (as in 6.1). Verify that the README's statements about `release/v17` match its workflows.
- [ ] 5.4 Smoke test the `release/v17` package `17.0.0` on a minimal Umbraco 17 site:
  - A deny rule returns 403 and an allow rule passes.
  - The dashboard renders.
  - Upgrading from `2.0.2` keeps the migration state at `1.1` and keeps existing rules.

## 6. Documentation and verification on main

- [x] 6.1 Update the README:
  - Requirements and version table (`18.x` maps to Umbraco 18 from `main`, `17.x` maps to Umbraco 17 from `release/v17`, `2.x` is the legacy Umbraco 17 line).
  - Branch policy and hotfix flow (fix on `main`, cherry-pick to `release/v17`; 17-only fixes directly on `release/v17`).
  - Build section without `-p:UmbracoMajor`.

  Verify that the documented commands match the workflows.
- [x] 6.2 Update `CLAUDE.md` (Umbraco 18 on `main`, the `release/v17` branch, the hotfix flow, `AsyncMigrationBase` for new migrations, OpenAPI via `Microsoft.AspNetCore.OpenApi`, and the actual API route `/umbraco/m1sterpl0wumbracoaccessrestriction/api/v1/...`). Update the `openspec/config.yaml` context. Verify that no remaining text mentions `-p:UmbracoMajor`, `#if UMBRACO` or `generate-client:17`.
- [x] 6.3 Confirm `umbraco-marketplace.json` `UmbracoVersion` stays at 17.0.0 (minimum supported across published packages). Verify that the file is unchanged.
- [x] 6.4 Run `npx @fission-ai/openspec validate --all --strict` and verify that it passes.

## Workflow follow-up

- Update PR #11 with the revised `main` changes and open the `release/v17` setup PR.
- Archive the change with `/opsx:archive` after both PRs merge.
- Publish `v17.0.0` targeting `release/v17` and `v18.0.0` targeting `main`, and check that both packages appear on NuGet with the correct dependency ranges.
