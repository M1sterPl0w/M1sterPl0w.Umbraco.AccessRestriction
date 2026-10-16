# Tasks

## 1. Umbraco major build switch

- [x] 1.1 Add `Directory.Build.props` at the repo root with `UmbracoMajor` (default `18`), `UmbracoNextMajor`, a validation error for values other than 17/18, and `DefineConstants` `UMBRACO_$(UmbracoMajor)`. Verify that `dotnet build -p:UmbracoMajor=16` fails with the validation error.
- [x] 1.2 Change every `Umbraco.Cms.*` entry in `Directory.Packages.props` to `[$(UmbracoMajor).0.0,$(UmbracoNextMajor).0.0)`. Verify with `dotnet restore -p:UmbracoMajor=17` and `-p:UmbracoMajor=18`: `obj/project.assets.json` resolves `Umbraco.Cms.Core` to 17.x and 18.x respectively.
- [x] 1.3 Add the version-major guard target (design D2). Verify that `dotnet pack -p:UmbracoMajor=18 -p:Version=17.1.0` fails with an error naming both majors. Verify that `-p:Version=18.0.0` and `-p:Version=0.0.0-ci` succeed.

## 2. Umbraco 18 compatibility

- [x] 2.1 Switch `InitialMigration` and `AddDenyResponseSettingsMigration` to `AsyncMigrationBase`/`MigrateAsync()` with identical steps (design D6a). Verify that both migration files compile with `-p:UmbracoMajor=17` and `-p:UmbracoMajor=18`, and that `AccessRestrictionMigrationPlan` is unchanged.
- [x] 2.2 Split the OpenAPI registration per major (design D6b): keep the Swashbuckle code under Umbraco 17, and for 18 add `AddBackOfficeOpenApiDocument` + `WithBackOfficeAuthentication` + an operation-ID transformer. Verify that `dotnet build` succeeds for both majors with no new compiler warnings, and fix any further errors that surface.
- [x] 2.3 Add xUnit tests for the Umbraco 18 operation-ID transformer (package controllers get the action name, others are untouched). Verify that `dotnet test -p:UmbracoMajor=17` and `dotnet test -p:UmbracoMajor=18` both pass. On a running site per major, compare the operation IDs in `/umbraco/swagger/m1sterpl0wumbracoaccessrestriction/swagger.json` (17) and `/umbraco/openapi/m1sterpl0wumbracoaccessrestriction.json` (18), and verify that they are identical.
- [ ] 2.4 Make `npm run generate-client` take the OpenAPI URL as an override (default the 18 URL), and document both URLs. Verify that it generates unchanged `src/api/*.gen.ts` against an 18 site.
- [x] 2.5 Inspect `dotnet pack -p:UmbracoMajor=18 -p:Version=18.0.0` output. Verify that the nuspec lists `Umbraco.Cms.*` dependencies as `[18.0.0, 19.0.0)`. Repeat for 17: `[17.0.0, 18.0.0)`.

## 3. Frontend

- [ ] 3.1 Bump `@umbraco-cms/backoffice` to `^18` in `Client/package.json` and refresh `package-lock.json` with `npm install`. Fix type errors in the dashboard element if any (do not hand-edit `src/api/*.gen.ts`). Verify that `npm run build` in `Client/` succeeds.

## 4. CI and release workflows

- [x] 4.1 Update `.github/workflows/ci.yml` with a matrix `umbraco: [17, 18]`. Pass `-p:UmbracoMajor=${{ matrix.umbraco }}` to restore, build and pack, and add a `dotnet test` step. Verify that the workflow YAML parses and that the commands run locally for both values.
- [x] 4.2 Update `.github/workflows/publish.yml` to derive `MAJOR` from the tag version and pass `-p:UmbracoMajor=$MAJOR` to restore, build and pack. Verify by running the same shell snippet locally with `GITHUB_REF_NAME=v18.0.0` (yields 18) and `v2.1.0`. The `v2.1.0` case must fail at restore.

## 5. Documentation and verification

- [x] 5.1 Update the README "Requirements" section with a version table (`17.x` maps to Umbraco 17, `18.x` maps to Umbraco 18, `2.x` is the legacy Umbraco 17 line). Add a note that minor and patch numbers are package-owned. Update `CLAUDE.md` (Umbraco pin and the `-p:UmbracoMajor` commands) and the `openspec/config.yaml` context line "Umbraco 17 / .NET 10". Verify that the documented commands match the workflows.
- [x] 5.2 Confirm `umbraco-marketplace.json` `UmbracoVersion` stays at 17.0.0 (minimum supported). Verify the file is unchanged or intentionally updated.
- [ ] 5.3 Smoke test: install a locally packed `17.0.0` into an Umbraco 17 site and `18.0.0` into an Umbraco 18 site. Verify on each that the dashboard loads, a deny rule blocks a request, and an allow rule passes. Also upgrade a 17 site's DB from `2.0.2` to `17.0.0` and verify that no migration re-runs and existing rules survive.
- [x] 5.4 Run `npx @fission-ai/openspec validate --all --strict` and verify it passes.

## Workflow follow-up

- Archive the change with `/opsx:archive` after merge.
- Publish releases `v17.0.0` and `v18.0.0` and check that both packages appear on NuGet with the correct dependency ranges.
