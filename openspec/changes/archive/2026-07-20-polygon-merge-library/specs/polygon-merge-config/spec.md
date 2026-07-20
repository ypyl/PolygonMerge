## ADDED Requirements

### Requirement: ParagraphGrouperOptions Configuration
The system SHALL define a `ParagraphGrouperOptions` class with the following configurable properties:

- `MinParagraphsPerPage` (int, default 6): The minimum target number of paragraphs per page.
- `MaxParagraphsPerPage` (int, default 20): The maximum target number of paragraphs per page.
- `VerticalDistanceWeight` (double, default 2.0): Weight multiplier for Y-axis distance in HAC.
- `MaxTableHorizontalGap` (double, default 25.0): Maximum X-gap (in points) for table row neighbor detection.
- `MaxTableVerticalGap` (double, default 15.0): Maximum Y-gap (in points) for table column neighbor detection.
- `EnableTableDetection` (bool, default true): Whether to run the table detection pre-pass.

All properties SHALL have sensible defaults so the consumer can use `new ParagraphGrouperOptions()` without additional configuration.

#### Scenario: Default options are production-usable
- **WHEN** a consumer creates `new ParagraphGrouperOptions()` with no customization
- **THEN** all properties have the documented default values

#### Scenario: Custom thresholds configured
- **WHEN** a consumer sets MaxParagraphsPerPage=12 and MaxTableHorizontalGap=30
- **THEN** the grouper targets at most 12 paragraphs per page and detects table rows with up to 30pt gaps

### Requirement: Options Validation
The system SHALL validate that MinParagraphsPerPage ≤ MaxParagraphsPerPage.

The system SHALL validate that MinParagraphsPerPage ≥ 1.

The system SHALL validate that all gap thresholds and weights are non-negative.

#### Scenario: Invalid range throws
- **WHEN** MinParagraphsPerPage=20 and MaxParagraphsPerPage=10
- **THEN** an ArgumentException is thrown during validation

#### Scenario: Valid options pass validation
- **WHEN** MinParagraphsPerPage=6 and MaxParagraphsPerPage=20
- **THEN** validation succeeds without exception

### Requirement: ParagraphGrouper Service Class
The system SHALL define a `ParagraphGrouper` class that accepts `ParagraphGrouperOptions` via constructor injection.

The class SHALL expose two `Group` method overloads:

**Overload 1 (basic):** `List<OcrParagraph> Group(List<OcrParagraph> paragraphs)`
1. Groups input paragraphs by Page.
2. For each page, optionally runs the heuristic table detection pre-pass (if `EnableTableDetection` is true).
3. Runs hierarchical agglomerative clustering to reduce clusters to the target range.
4. Returns the merged paragraphs as a flat list across all pages.

**Overload 2 (with table structure):** `List<OcrParagraph> Group(List<OcrParagraph> paragraphs, List<TableCellInfo> tableCells, Func<OcrParagraph, string> paragraphKeySelector)`
1. Groups input paragraphs by Page.
2. For each page, merges table cells by (TableId, Row) into locked clusters using the provided table structure metadata.
3. Runs hierarchical agglomerative clustering to reduce clusters to the target range.
4. Returns the merged paragraphs as a flat list across all pages.

When Overload 2 is used, the heuristic `EnableTableDetection` option SHALL be ignored (explicit structure takes precedence).

#### Scenario: Full pipeline with explicit table structure
- **WHEN** Group(paragraphs, tableCells, keySelector) is called with 50 paragraphs on a single page containing a 2×2 table (2 rows × 2 columns)
- **THEN** each row's cells are merged into locked clusters (2 locked clusters), HAC reduces remaining clusters to ≤20, and the result is a list of OcrParagraph instances

#### Scenario: Full pipeline with heuristic table detection
- **WHEN** Group(paragraphs) is called with 50 paragraphs on a single page containing a tight 2×2 grid and EnableTableDetection=true
- **THEN** the grid is detected heuristically and merged into one locked zone, HAC reduces remaining clusters to ≤20

#### Scenario: Pipeline with table detection disabled
- **WHEN** Group(paragraphs) is called with EnableTableDetection=false
- **THEN** the table detection pre-pass is skipped and only HAC runs

### Requirement: Dependency Injection Support
The system SHALL support registration via `Microsoft.Extensions.DependencyInjection` using a `services.AddParagraphGrouper(Action<ParagraphGrouperOptions> configure)` extension method.

The registration SHALL use Singleton lifetime by default.

#### Scenario: DI registration with defaults
- **WHEN** `services.AddParagraphGrouper()` is called in a service collection
- **THEN** `IParagraphGrouper` is registered as a singleton with default options

#### Scenario: DI registration with custom options
- **WHEN** `services.AddParagraphGrouper(opts => opts.MaxParagraphsPerPage = 10)` is called
- **THEN** the registered grouper uses the customized options

### Requirement: IParagraphGrouper Interface
The system SHALL define an `IParagraphGrouper` interface with two methods:
- `List<OcrParagraph> Group(List<OcrParagraph> paragraphs)`
- `List<OcrParagraph> Group(List<OcrParagraph> paragraphs, List<TableCellInfo> tableCells, Func<OcrParagraph, string> paragraphKeySelector)`

The `ParagraphGrouper` class SHALL implement this interface.

#### Scenario: Interface abstraction for testability
- **WHEN** a consumer depends on IParagraphGrouper via constructor injection
- **THEN** they can substitute a mock implementation in unit tests
