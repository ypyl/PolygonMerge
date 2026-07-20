# ocr-model Specification

## Purpose
Defines the input/output data models for OCR paragraph data: `OcrParagraph`, `Polygon` (axis-aligned bounding box with merge and distance utilities), and `TableCellInfo` (table cell metadata from upstream OCR engines). These models form the data contract for all PolygonMerge operations and support `System.Text.Json` serialization.
## Requirements
### Requirement: OcrParagraph Input Model
The system SHALL define an `OcrParagraph` class with the following properties:

- `Page` (int): The page number the paragraph belongs to.
- `Content` (string): The text content of the paragraph.
- `BoundingBox` (Polygon): The bounding polygon of the paragraph.

All properties SHALL be read/write to support JSON deserialization.

#### Scenario: Deserialize from JSON
- **WHEN** JSON input `{"page": 1, "content": "Hello World", "boundingBox": {"minX": 0, "minY": 0, "maxX": 100, "maxY": 10}}` is deserialized
- **THEN** an OcrParagraph is created with Page=1, Content="Hello World", and BoundingBox=(0,0)-(100,10)

### Requirement: Polygon Model
The system SHALL define a `Polygon` class (axis-aligned bounding box) with properties:

- `MinX` (double): Left edge X coordinate.
- `MinY` (double): Top edge Y coordinate.
- `MaxX` (double): Right edge X coordinate.
- `MaxY` (double): Bottom edge Y coordinate.

The Polygon class SHALL include:
- A static `Merge(Polygon a, Polygon b)` method returning the union bounding box.
- A `DistanceTo(Polygon other, double verticalWeight = 2.0)` method computing the weighted AABB distance.

#### Scenario: Polygon merge returns union
- **WHEN** Polygon.Merge((0,0)-(10,10), (5,5)-(15,15)) is called
- **THEN** the result is Polygon(0,0)-(15,15)

#### Scenario: DistanceTo with vertical weight
- **WHEN** box A (0,0)-(10,10) calls DistanceTo(box B (0,15)-(10,25), verticalWeight: 2.0)
- **THEN** the result is 10.0

### Requirement: OcrParagraph Output
The merged output SHALL use the same `OcrParagraph` class as input.

The output paragraph's `Page` SHALL be the page of the source paragraphs.
The output paragraph's `Content` SHALL be the space-joined content of all source paragraphs in reading order.
The output paragraph's `BoundingBox` SHALL be the merged bounding box of all source paragraphs.

#### Scenario: Output preserves same model type
- **WHEN** three OcrParagraph inputs are merged into one
- **THEN** the result is a single OcrParagraph with Page, Content, and BoundingBox set from the merged cluster

### Requirement: TableCellInfo Model
The system SHALL define a `TableCellInfo` class with the following properties:

- `ParagraphId` (string): Identifies which OcrParagraph this cell corresponds to, matched via a user-provided key selector.
- `Row` (int): Zero-based row index within the table.
- `Column` (int): Zero-based column index within the table.
- `TableId` (string, default null): Optional table identifier to distinguish multiple tables on the same page.

All properties SHALL be read/write to support JSON deserialization.

#### Scenario: Deserialize TableCellInfo from JSON
- **WHEN** JSON `{"paragraphId": "p42", "row": 0, "column": 1, "tableId": "t1"}` is deserialized
- **THEN** a TableCellInfo is created with ParagraphId="p42", Row=0, Column=1, TableId="t1"

### Requirement: JSON Serialization Contract
All model classes SHALL support `System.Text.Json` serialization and deserialization using default property-name casing (camelCase for JSON, PascalCase for C# via `JsonPropertyName` attributes or equivalent).

#### Scenario: Round-trip serialization
- **WHEN** an OcrParagraph is serialized to JSON and then deserialized back
- **THEN** all property values are preserved exactly

#### Scenario: TableCellInfo round-trip serialization
- **WHEN** a TableCellInfo is serialized to JSON and then deserialized back
- **THEN** all property values are preserved exactly

