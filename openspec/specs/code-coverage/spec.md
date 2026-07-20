# code-coverage Specification

## Purpose
Enforces 100% line and branch code coverage on the PolygonMerge library using Coverlet, integrated via `dotnet test` with threshold enforcement. Defines exclusion rules for non-testable DI glue code and the single-command coverage verification workflow.
## Requirements
### Requirement: Coverlet Package Integration
The test project SHALL reference the `coverlet.collector` NuGet package for code coverage instrumentation.

The package version SHALL be the latest stable release compatible with .NET 10.

#### Scenario: Coverage collection runs during dotnet test
- **WHEN** `dotnet test --collect:"XPlat Code Coverage"` is executed
- **THEN** a coverage report file (coverage.cobertura.xml) is generated in the TestResults directory

### Requirement: Coverage Threshold Enforcement
The test project SHALL enforce a 100% line coverage threshold and a 100% branch coverage threshold.

The thresholds SHALL be configured via a `.runsettings` file referenced by the test project.

#### Scenario: Build fails when coverage drops below threshold
- **WHEN** `dotnet test` runs with coverage and any line or branch falls below 100%
- **THEN** the test run fails with a threshold violation error

#### Scenario: Build passes when coverage meets threshold
- **WHEN** `dotnet test` runs with coverage and all lines and branches are at 100%
- **THEN** the test run passes successfully

### Requirement: Non-Testable Code Exclusion
The `ParagraphGrouperServiceExtensions` class SHALL be decorated with `[ExcludeFromCodeCoverage]` to exclude DI glue code from coverage calculations.

All core library logic (clustering, table detection, distance calculations, models) SHALL remain subject to coverage enforcement.

#### Scenario: DI glue code excluded from coverage
- **WHEN** coverage is measured
- **THEN** the `AddParagraphGrouper` method is not counted against coverage thresholds

### Requirement: Coverage Verification Command
The repository SHALL support a single command to run tests with coverage and enforce thresholds:

```bash
dotnet test --collect:"XPlat Code Coverage" --settings tests/PolygonMerge.Tests/.runsettings
```

#### Scenario: Single command for coverage check
- **WHEN** the coverage command is executed
- **THEN** tests run, coverage is collected, thresholds are checked, and results are reported in a single pass

