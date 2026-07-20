## ADDED Requirements

### Requirement: TableCellInfo Model
The system SHALL define a `TableCellInfo` class with the following properties:

- `ParagraphId` (string or int): Identifies which `OcrParagraph` this cell corresponds to. The matching mechanism SHALL be configurable (by index, by a user-provided key, or by position in the input list).
- `Row` (int): The zero-based row index within the table.
- `Column` (int): The zero-based column index within the table.
- `TableId` (string, optional): An identifier for the table. When multiple tables exist on a page, cells with different TableId values SHALL NOT be merged together. When null or absent, all cells on the same page are treated as belonging to one logical table.

#### Scenario: Deserialize table cell info
- **WHEN** JSON `{"paragraphId": "p42", "row": 0, "column": 1, "tableId": "t1"}` is deserialized
- **THEN** a TableCellInfo is created with ParagraphId="p42", Row=0, Column=1, TableId="t1"

### Requirement: Paragraph Identification
The system SHALL support matching `TableCellInfo` entries to `OcrParagraph` instances via a user-provided key selector function.

The `Group` method overload that accepts table structure SHALL take a `Func<OcrParagraph, string> paragraphKeySelector` parameter.

#### Scenario: Match by custom ID
- **WHEN** OcrParagraph list contains an item with a custom ID "p42" and TableCellInfo has ParagraphId "p42"
- **THEN** the system matches them using the key selector `p => p.Id`

#### Scenario: Match by index
- **WHEN** the consumer wants to match by list position and provides a key selector that returns the index
- **THEN** the system matches TableCellInfo entries to OcrParagraph instances at the corresponding position

### Requirement: Row-Level Table Merging
The system SHALL merge all `OcrParagraph` instances that belong to the same `(TableId, Row, Page)` into a single locked cluster.

Paragraphs within the same row SHALL be ordered by Column ascending when concatenating content.

#### Scenario: Merge 3 cells in a table row
- **WHEN** three TableCellInfo entries reference paragraphs "a", "b", "c" all on page 1, table "t1", row 0, with columns 0, 1, 2 respectively
- **THEN** paragraphs "a", "b", "c" are merged into one locked cluster with content "a b c" in column order

#### Scenario: Different rows stay separate
- **WHEN** TableCellInfo entries reference paragraphs for row 0 columns 0-1 and row 1 columns 0-1 of the same table
- **THEN** two separate locked clusters are created (one per row), not merged together

#### Scenario: Different tables stay separate
- **WHEN** TableCellInfo entries reference paragraphs for table "t1" row 0 and table "t2" row 0 on the same page
- **THEN** two separate locked clusters are created, one per table

### Requirement: Locked Cluster Marking
Each row-merged table cluster SHALL be marked as locked so the HAC pass treats it as an atomic unit.

Non-table paragraphs (those not referenced by any TableCellInfo) SHALL remain as free (unlocked) clusters.

#### Scenario: Table rows are locked, body text is free
- **WHEN** a page has 2 table rows (4 cells) and 10 body paragraphs, and table structure is provided
- **THEN** the pipeline produces 2 locked clusters (one per row) and 10 free clusters before HAC runs

### Requirement: Cross-Page Table Isolation
Table rows SHALL be scoped to the page of their constituent paragraphs.

Cells referencing paragraphs on different pages SHALL NOT be merged together, even if they share the same TableId and Row.

#### Scenario: Same table spanning two pages
- **WHEN** table "t1" has rows 0-3 on page 1 and rows 4-7 on page 2
- **THEN** rows 0-3 form locked clusters on page 1, rows 4-7 form locked clusters on page 2, and no cross-page merging occurs
