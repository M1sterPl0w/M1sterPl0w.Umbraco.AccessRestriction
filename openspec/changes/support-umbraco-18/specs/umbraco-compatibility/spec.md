# Spec Delta

## Purpose

Define which Umbraco versions each package release supports, how the package version communicates that, and which branch maintains each Umbraco major, so site owners can pick the right release for their Umbraco major.

## ADDED Requirements

### Requirement: Supported Umbraco majors
The project SHALL publish installable packages for Umbraco 17 and Umbraco 18. `main` SHALL produce the Umbraco 18 packages, and the `release/v17` branch SHALL produce the Umbraco 17 packages.

#### Scenario: Install on Umbraco 17
- **WHEN** a site on Umbraco 17.x installs the latest `17.*` package
- **THEN** NuGet restore succeeds and the access restriction middleware and dashboard work

#### Scenario: Install on Umbraco 18
- **WHEN** a site on Umbraco 18.x installs the latest `18.*` package
- **THEN** NuGet restore succeeds and the access restriction middleware and dashboard work

### Requirement: Package major matches Umbraco major
A package whose version major is N SHALL depend only on Umbraco major N. Its Umbraco dependency range SHALL include every N.x release and exclude N+1.0.0 and later.

#### Scenario: 18.x package dependency range
- **WHEN** the `18.0.0` package's nuspec is inspected
- **THEN** each `Umbraco.Cms.*` dependency allows `18.0.0` and later 18.x versions and excludes `19.0.0`

#### Scenario: 17.x package on Umbraco 18
- **WHEN** a site on Umbraco 18 tries to install a `17.*` package
- **THEN** NuGet reports a version conflict instead of installing it

### Requirement: Package minor and patch are independent
The package minor and patch numbers SHALL belong to this package and SHALL NOT have to equal the Umbraco minor or patch.

#### Scenario: Package fix release
- **WHEN** a bug fix ships for Umbraco 18 users while Umbraco is at 18.2.1
- **THEN** the package can be released as `18.0.1`

### Requirement: Each branch builds one Umbraco major
Each maintained branch SHALL build against exactly one Umbraco major, fixed in that branch's build configuration, without conditional compilation per major.

#### Scenario: Build on main
- **WHEN** `dotnet build` runs on `main` without extra properties
- **THEN** it restores and compiles against Umbraco 18.x

#### Scenario: Build on release/v17
- **WHEN** `dotnet build` runs on `release/v17` without extra properties
- **THEN** it restores and compiles against Umbraco 17.x

### Requirement: Mismatched version is rejected at pack time
Packing with a version whose major differs from the branch's Umbraco major SHALL fail the build. CI dry-run versions (`0.0.0-ci`) are exempt.

#### Scenario: Wrong major
- **WHEN** the package is packed on `main` with version `17.1.0`
- **THEN** pack fails with an error naming both majors

#### Scenario: CI dry-run version
- **WHEN** CI packs with version `0.0.0-ci`
- **THEN** pack succeeds on either branch

### Requirement: Release is built from its major's branch
A release tagged `vN.x.y` SHALL be built from the branch that maintains Umbraco major N. A release whose tag major does not equal that branch's Umbraco major SHALL fail without publishing.

#### Scenario: Tag v18.0.0 on main
- **WHEN** release `v18.0.0` is published from `main`
- **THEN** package `18.0.0` built against Umbraco 18 is pushed to NuGet

#### Scenario: Tag targets the wrong branch
- **WHEN** release `v17.0.1` is published from `main`
- **THEN** the workflow fails and nothing is pushed to NuGet

#### Scenario: Unsupported tag major
- **WHEN** release `v2.1.0` is published from either branch
- **THEN** the workflow fails and nothing is pushed to NuGet

### Requirement: Each branch verified on every change
CI SHALL build, test and pack-dry-run each maintained branch against its own Umbraco major on every push and pull request to that branch.

#### Scenario: Pull request to release/v17
- **WHEN** a pull request targets `release/v17` and fails to compile against Umbraco 17
- **THEN** the CI run for that pull request fails

### Requirement: Hotfix flow between branches
A fix that applies to both majors SHALL land on `main` first and then be cherry-picked to `release/v17`. A fix that only affects Umbraco 17 SHALL land on `release/v17` directly.

#### Scenario: Shared bug
- **WHEN** a rule-engine bug affects both Umbraco 17 and 18
- **THEN** the fix merges to `main`, a cherry-pick merges to `release/v17`, and the fix ships as both `18.x.y` and `17.x.y`

#### Scenario: Umbraco 17-only bug
- **WHEN** a bug only reproduces on Umbraco 17
- **THEN** the fix merges to `release/v17` only and ships as a `17.x.y` release
