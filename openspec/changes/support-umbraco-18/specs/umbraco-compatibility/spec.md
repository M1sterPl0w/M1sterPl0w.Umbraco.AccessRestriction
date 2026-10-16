# Spec Delta

## Purpose

Define which Umbraco versions each package release supports and how the package version communicates that, so site owners can pick the right release for their Umbraco major.

## ADDED Requirements

### Requirement: Supported Umbraco majors
The project SHALL produce installable packages for Umbraco 17 and Umbraco 18 from the same source.

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

### Requirement: Release tag selects the Umbraco major
A release SHALL build and pack against the Umbraco major equal to the major of its tag (`vN.x.y`). A release whose tag major is not a supported Umbraco major SHALL fail without publishing.

#### Scenario: Tag v18.0.0
- **WHEN** release `v18.0.0` is published
- **THEN** package `18.0.0` built against Umbraco 18 is pushed to NuGet

#### Scenario: Unsupported tag major
- **WHEN** release `v2.1.0` is published
- **THEN** the workflow fails and nothing is pushed to NuGet

### Requirement: Mismatched version is rejected at pack time
Packing with a version whose major differs from the selected Umbraco major SHALL fail the build. CI dry-run versions (`0.0.0-ci`) are exempt.

#### Scenario: Wrong major
- **WHEN** the package is packed with version `17.1.0` while Umbraco 18 is selected
- **THEN** pack fails with an error naming both majors

#### Scenario: CI dry-run version
- **WHEN** CI packs with version `0.0.0-ci`
- **THEN** pack succeeds for either selected Umbraco major

### Requirement: Both majors verified on every change
CI SHALL build, test and pack-dry-run the package against each supported Umbraco major for every push and pull request to `main`.

#### Scenario: Break only on Umbraco 17
- **WHEN** a pull request compiles against Umbraco 18 but not against Umbraco 17
- **THEN** the CI run for that pull request fails
