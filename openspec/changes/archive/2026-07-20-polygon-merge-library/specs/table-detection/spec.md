## ADDED Requirements

### Requirement: Grid Adjacency Table Detection
The system SHALL detect table structures by identifying paragraphs that share row or column alignment and are within configurable gap thresholds.

The detection SHALL use edge-to-edge distance calculations. Polygons that overlap on an axis produce zero gap on that axis.

#### Scenario: Side-by-side cells in same table row detected
- **WHEN** cell A at (0,10)-(50,20) and cell B at (55,10)-(105,20) with MaxHorizontalGap=25
- **THEN** the system identifies them as table neighbors (shares Y-range, X-gap=5 ≤ 25)

#### Scenario: Stacked cells in same table column detected
- **WHEN** cell A at (0,10)-(50,20) and cell B at (0,22)-(50,32) with MaxVerticalGap=15
- **THEN** the system identifies them as table neighbors (shares X-range, Y-gap=2 ≤ 15)

#### Scenario: Overlapping cells detected as table neighbors
- **WHEN** cell A at (0,10)-(50,25) and cell B at (40,12)-(90,23) — they overlap on both axes (gap=0 on both)
- **THEN** the system identifies them as table neighbors (shares both alignments, gaps are 0 ≤ thresholds)

#### Scenario: Non-table paragraphs not falsely detected
- **WHEN** two body paragraphs are far apart horizontally (X-gap=50 > MaxHorizontalGap=25) even if they share Y-range
- **THEN** the system does NOT identify them as table neighbors

#### Scenario: Diagonal neighbors not detected
- **WHEN** cell A at (0,0)-(10,10) and cell B at (20,20)-(30,30) — neither shares row nor column alignment
- **THEN** the system does NOT identify them as table neighbors

### Requirement: Table Zone Collapse
The system SHALL iteratively merge all detected table-neighbor paragraphs into unified table zone clusters.

The merge SHALL be transitive: if A is a table neighbor of B, and B is a table neighbor of C, then A, B, and C all end up in the same table zone cluster.

#### Scenario: 2×2 grid collapses into single table zone
- **WHEN** four cells form a 2×2 grid where each cell is a table neighbor of at least one other cell
- **THEN** all four cells are collapsed into a single table zone cluster

#### Scenario: Two separate tables on same page
- **WHEN** page contains table T1 (4 cells in a grid, tight spacing) and table T2 (6 cells in a grid, tight spacing) separated by 100pt of body text
- **THEN** T1 cells form one locked cluster and T2 cells form a separate locked cluster

### Requirement: Configurable Gap Thresholds
The system SHALL accept configurable maximum horizontal gap and maximum vertical gap parameters for table neighbor detection.

Default values SHALL be MaxHorizontalGap=25 and MaxVerticalGap=15 (in document points).

#### Scenario: Wide table detected with increased threshold
- **WHEN** cells have 30pt X-gap and MaxHorizontalGap is configured to 35
- **THEN** the cells are detected as table neighbors

#### Scenario: Wide table missed with default threshold
- **WHEN** cells have 30pt X-gap and thresholds are at defaults (MaxHorizontalGap=25)
- **THEN** the cells are NOT detected as table neighbors

### Requirement: Table Zone Output Marking
Each cluster produced by the table detection pass SHALL be marked as "locked" so the subsequent HAC pass treats it as an atomic unit.

#### Scenario: Table zone cluster is locked
- **WHEN** the table detection pass produces a merged cluster of 4 table cells
- **THEN** that cluster's IsLocked property is true
