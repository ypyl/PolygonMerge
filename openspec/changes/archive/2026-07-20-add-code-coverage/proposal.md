## Why

The PolygonMerge library has 36 unit tests covering all spec-defined scenarios, but there is no enforcement mechanism to ensure coverage stays at 100% as the codebase evolves. Without coverage gates, future changes can introduce untested code paths. Coverlet provides line and branch coverage instrumentation for .NET with threshold enforcement, ensuring every code path remains tested.

## What Changes

- Add Coverlet NuGet package to the test project for code coverage instrumentation.
- Configure 100% line and branch coverage thresholds in the test project — build fails if coverage drops.
- Exclude non-testable surface area from coverage (e.g., DI extension methods that are inherently integration-level, model property getters/setters covered by serialization tests).
- Fix any coverage gaps revealed by initial instrumentation to hit 100%.

## Capabilities

### New Capabilities

- `code-coverage`: Enforces 100% line and branch code coverage on the PolygonMerge library using Coverlet, integrated into the CI/build pipeline via `dotnet test` with coverage thresholds.

### Modified Capabilities

<!-- No existing capabilities modified — this is a tooling/infrastructure change. -->

## Impact

- `tests/PolygonMerge.Tests/PolygonMerge.Tests.csproj`: Add `coverlet.collector` package reference and coverage threshold configuration.
- `src/PolygonMerge/PolygonMerge.csproj`: No changes (coverage is measured from test project).
- Build pipeline: `dotnet test` now includes coverage collection and threshold enforcement.
- No API or behavioral changes to the library.
