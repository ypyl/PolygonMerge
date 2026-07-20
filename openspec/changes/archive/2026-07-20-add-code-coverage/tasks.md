## 1. Add Coverlet Package

- [x] 1.1 Add `coverlet.collector` NuGet package to `tests/PolygonMerge.Tests/PolygonMerge.Tests.csproj`

## 2. Configure Coverage Thresholds

- [x] 2.1 Create `.runsettings` file in `tests/PolygonMerge.Tests/` with 100% line and branch thresholds
- [x] 2.2 Reference `.runsettings` in the test project (via `<RunSettingsFilePath>` in `.csproj`)

## 3. Exclude DI Glue Code

- [x] 3.1 Add `[ExcludeFromCodeCoverage]` attribute to `ParagraphGrouperServiceExtensions` class

## 4. Verify and Fix Coverage Gaps

- [x] 4.1 Run `dotnet test` with coverage collection and check for threshold failures
- [x] 4.2 Fix any uncovered lines or branches revealed by coverage instrumentation
- [x] 4.3 Verify final coverage reaches 100% line and branch on the library project
