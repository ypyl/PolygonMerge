## Context

The PolygonMerge library currently has 36 xUnit tests but no coverage measurement or enforcement. Coverage is checked manually/ad-hoc. Adding Coverlet provides automated instrumentation with threshold gates in the build pipeline. The library is small (~300 lines of production code) making 100% coverage a reasonable and achievable target.

## Goals / Non-Goals

**Goals:**
- Add Coverlet coverage collection to the test project.
- Enforce 100% line coverage and 100% branch coverage thresholds.
- Build fails if coverage drops below threshold.
- Coverage report generated during `dotnet test`.
- Exclude non-testable boilerplate (e.g., `[JsonPropertyName]` attributes, DI registration glue) from coverage calculations where appropriate.

**Non-Goals:**
- Integration or end-to-end tests — this is unit-test coverage only.
- Coverage for the test project itself.
- Publishing coverage reports to external services (e.g., Coveralls, Codecov).
- Mutation testing.

## Decisions

### Decision 1: Coverlet Collector (Not Coverlet MSBuild)

**Choice:** Use the `coverlet.collector` NuGet package (the VSTest data collector approach), not `coverlet.msbuild`.

**Rationale:** `coverlet.collector` is the recommended approach for `dotnet test`. It integrates via `--collect:"XPlat Code Coverage"` and works with `dotnet test` without MSBuild target coupling. It's simpler to configure and is actively maintained as part of the Coverlet project.

**Alternatives considered:**
- `coverlet.msbuild`: Works via MSBuild targets. Requires `<CoverletOutputFormat>` and `<CollectCoverage>` MSBuild properties. More verbose configuration, but same underlying engine. Rejected in favor of the simpler collector approach.
- `Microsoft.CodeCoverage` (built-in): Only available in Visual Studio Enterprise. Not portable.

### Decision 2: Threshold Enforcement via .runsettings

**Choice:** Configure line and branch coverage thresholds in a `.runsettings` file referenced by the test project.

**Rationale:** Coverlet respects the `CoverletRunSettings.xml` or `.runsettings` format for threshold configuration. This keeps threshold values in a committed file rather than CLI arguments that could be forgotten.

```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Threshold>100,100,100,100</Threshold>
          <ThresholdType>line,branch,method,class</ThresholdType>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### Decision 3: Exclude DI Extension Method from Coverage

**Choice:** Add `[ExcludeFromCodeCoverage]` attribute to the `ParagraphGrouperServiceExtensions` class.

**Rationale:** The `AddParagraphGrouper()` extension method is pure glue code — it creates a `ParagraphGrouperOptions`, calls `services.AddSingleton`, etc. It's inherently integration-level and testing it requires building a service provider (which the DI tests already do). The DI registration is already tested via the `DiRegistrationTests` class. The `[ExcludeFromCodeCoverage]` attribute excludes it from line/branch coverage calculations while keeping it tested.

### Decision 4: 100% Threshold on Line and Branch Only

**Choice:** Set thresholds to 100% for line and branch coverage. Method and class coverage are derivative metrics — if every line is covered, all methods and classes are covered by definition.

**Rationale:** Line + branch is the meaningful metric. Setting all four to 100% is clean and doesn't hurt anything, but line and branch are what matter.

## Risks / Trade-offs

- **[Risk] 100% branch coverage can be pedantic:** Switch expressions and ternary operators generate branches that all need exercising. → **Mitigation:** Review if any branches are unreachable dead code — if so, remove them. If they're reachable but hard to hit, add targeted tests.
- **[Risk] Exclusions can become a slipperly slope:** Over-use of `[ExcludeFromCodeCoverage]` defeats the purpose. → **Mitigation:** Only the DI extension method is excluded. All core logic (clustering, table detection, distance calculations) remains under coverage enforcement.
