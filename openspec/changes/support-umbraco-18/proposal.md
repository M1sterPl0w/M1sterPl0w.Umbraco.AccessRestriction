# Proposal

## Why

The package only installs on Umbraco 17 (`Umbraco.Cms.*` pinned to `[17.0.0,17.999)`), while Umbraco 18 is released (18.0.0 through 18.2.1 on NuGet). Sites that upgrade to Umbraco 18 cannot use the package. The package version (currently `2.0.2`) also gives users no hint which Umbraco major it works with.

## What Changes

- Build the package for both Umbraco 17 and Umbraco 18 from one source tree. A build switch selects the Umbraco major. Both Umbraco 17 and 18 target `net10.0`, so the target framework cannot make the selection.
- Align the package major version with the Umbraco major: `17.x.y` depends on Umbraco 17 only, and `18.x.y` depends on Umbraco 18 only. Package minor and patch numbers stay owned by this package. They do not track Umbraco minors.
- **BREAKING** (versioning only): the next releases jump from `2.0.2` to `17.0.0` and `18.0.0`. Runtime behavior and the public API do not change.
- Release workflow: the tag's major (`v17.0.0` / `v18.0.0`) selects the Umbraco major to build and pack against. A tag whose major is not a supported Umbraco major fails the release.
- CI builds, tests and pack-dry-runs both Umbraco majors.
- README documents the package-to-Umbraco version mapping.

## Capabilities

### New Capabilities
- `umbraco-compatibility`: which Umbraco versions a package release supports, how the package version relates to the Umbraco major, and how a release picks its Umbraco major.

### Modified Capabilities

None. Request handling, rule engine, conditions and rule management keep their behavior on both majors.

## Impact

- `Directory.Packages.props`: the Umbraco version range becomes driven by the selected major instead of hard-coded 17.
- A new MSBuild property (the Umbraco major) with a guard that version major equals Umbraco major at pack time.
- `.github/workflows/ci.yml`: matrix over Umbraco 17 and 18, plus `dotnet test`.
- `.github/workflows/publish.yml` (release workflow): derive the Umbraco major from the tag.
- C# sources: a trial build against Umbraco 18 fails in two areas. Migrations (`MigrationBase` was removed) move to `AsyncMigrationBase`, which works on both majors. OpenAPI registration (Umbraco 18 dropped Swashbuckle for `Microsoft.AspNetCore.OpenApi`) needs separate code per major.
- `generate-client`: the OpenAPI URL differs per major.
- `Client/`: the `@umbraco-cms/backoffice` dev dependency (types only; runtime is external) must type-check for both majors.
- `README.md` requirements section, `umbraco-marketplace.json` (stays at minimum Umbraco 17).
- Out of scope: the stale csproj `<Description>` and the stale README tail section.
