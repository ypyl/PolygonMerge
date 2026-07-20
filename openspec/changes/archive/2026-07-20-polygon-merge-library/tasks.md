## 1. Project Scaffolding

- [x] 1.1 Create solution file `PolygonMerge.sln` at repo root
- [x] 1.2 Create class library project `src/PolygonMerge/PolygonMerge.csproj` targeting `net10.0`
- [x] 1.3 Create xUnit test project `tests/PolygonMerge.Tests/PolygonMerge.Tests.csproj` targeting `net10.0`
- [x] 1.4 Add project reference from test project to class library
- [x] 1.5 Add solution folders and add both projects to solution
- [x] 1.6 Configure NuGet package metadata in `PolygonMerge.csproj` (PackageId, Version, Description, Authors, License, RepositoryUrl)

## 2. Data Models

- [x] 2.1 Implement `Polygon` class with MinX, MinY, MaxX, MaxY properties and JSON serialization attributes
- [x] 2.2 Implement `Polygon.Merge(Polygon a, Polygon b)` static method returning union bounding box
- [x] 2.3 Implement `Polygon.DistanceTo(Polygon other, double verticalWeight = 2.0)` method with AABB weighted distance
- [x] 2.4 Implement `Polygon.Merge(IEnumerable<Polygon>)` overload accepting multiple polygons
- [x] 2.5 Implement `OcrParagraph` class with Page, Content, BoundingBox properties and JSON serialization attributes
- [x] 2.6 Implement `TableCellInfo` class with ParagraphId, Row, Column, TableId properties and JSON serialization attributes

## 3. Configuration & Service Interface

- [x] 3.1 Implement `ParagraphGrouperOptions` class with all configurable properties and documented defaults
- [x] 3.2 Add options validation logic (Min ≤ Max, positive values, non-negative thresholds)
- [x] 3.3 Implement `IParagraphGrouper` interface with two `Group` method overloads
- [x] 3.4 Implement `ParagraphGrouper` class skeleton with constructor accepting `ParagraphGrouperOptions`
- [x] 3.5 Implement `AddParagraphGrouper` DI extension method on `IServiceCollection`

## 4. Core Clustering Engine

- [x] 4.1 Implement internal `Cluster` class (holds list of OcrParagraph, IsLocked flag, and cached total polygon)
- [x] 4.2 Implement cluster merging logic (concatenate content in reading order, merge polygons)
- [x] 4.3 Implement hierarchical agglomerative clustering loop with pairwise distance search
- [x] 4.4 Implement locked-cluster constraint in HAC (skip merging two locked clusters)
- [x] 4.5 Implement per-page input grouping (partition by Page property before clustering)

## 5. Table Structure Pre-Pass (Explicit)

- [x] 5.1 Implement paragraph-to-TableCellInfo matching using key selector function
- [x] 5.2 Implement row-level merging: group matched paragraphs by (Page, TableId, Row), merge each row into a locked cluster with content ordered by Column
- [x] 5.3 Handle missing key matches gracefully (unmatched TableCellInfo entries are ignored; unmatched paragraphs remain free)

## 6. Table Detection Pre-Pass (Heuristic Fallback)

- [x] 6.1 Implement `IsTableNeighbor(Polygon a, Polygon b, double maxHorizontalGap, double maxVerticalGap)` method
- [x] 6.2 Implement transitive table zone collapse (iteratively merge all connected table-neighbor cells into one locked cluster)
- [x] 6.3 Mark resulting table zones as locked clusters

## 7. Main Pipeline Integration

- [x] 7.1 Implement basic `Group(paragraphs)` overload: wire heuristic table detection (gated by `EnableTableDetection`) → HAC reduction
- [x] 7.2 Implement `Group(paragraphs, tableCells, keySelector)` overload: wire explicit table structure pre-pass → HAC reduction (ignores `EnableTableDetection`)
- [x] 7.3 Wire HAC reduction into both overloads with target count from options
- [x] 7.4 Flatten clustered results back to `List<OcrParagraph>` output
- [x] 7.5 Add guard clause for null/empty input returning empty list

## 8. Unit Tests

- [x] 8.1 Test `Polygon.Merge` for two and multiple polygons
- [x] 8.2 Test `Polygon.DistanceTo` for non-overlapping side-by-side, stacked, overlapping, and diagonal cases
- [x] 8.3 Test `IsTableNeighbor` for same-row, same-column, non-table, and diagonal scenarios
- [x] 8.4 Test heuristic table zone collapse: 2×2 grid → single locked cluster, two separate tables → two clusters
- [x] 8.5 Test explicit table structure: same-row cells merged into one locked cluster, different rows stay separate, different tables stay separate
- [x] 8.6 Test explicit table structure: content ordered by Column within a row
- [x] 8.7 Test explicit table structure: cross-page isolation (same TableId on different pages do not merge)
- [x] 8.8 Test explicit table structure: unmatched TableCellInfo entries are ignored, unmatched paragraphs remain free
- [x] 8.9 Test HAC reduction: 50→20, already-in-range (8→8 unchanged), below-minimum (3→3 unchanged)
- [x] 8.10 Test locked cluster constraint: two locked clusters are not merged together
- [x] 8.11 Test content ordering after merge (top-to-bottom, left-to-right)
- [x] 8.12 Test per-page isolation: paragraphs from different pages never merge
- [x] 8.13 Test full pipeline end-to-end with explicit table structure
- [x] 8.14 Test full pipeline end-to-end with heuristic table detection enabled and disabled
- [x] 8.15 Test `ParagraphGrouperOptions` validation (invalid range throws, valid passes)
- [x] 8.16 Test DI registration resolves `IParagraphGrouper` with default and custom options
- [x] 8.17 Test JSON round-trip serialization of `OcrParagraph`, `Polygon`, and `TableCellInfo`

## 9. NuGet Packaging

- [x] 9.1 Verify `dotnet pack` produces a valid `.nupkg`
- [x] 9.2 Add README.md with usage examples at repo root and as package readme
- [x] 9.3 Add XML doc comments on all public API surface
