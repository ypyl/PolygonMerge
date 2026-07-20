## ADDED Requirements

### Requirement: Hierarchical Agglomerative Clustering
The system SHALL merge a list of paragraph clusters into a target number of clusters using hierarchical agglomerative clustering with axis-aligned bounding box (AABB) distance.

The algorithm SHALL iteratively find the two closest clusters, merge them, and repeat until the cluster count is within the configured target range.

#### Scenario: Reduce clusters to target count
- **WHEN** input contains 50 paragraph clusters and the target count is configured as 6 to 20
- **THEN** the system merges clusters iteratively until exactly 20 clusters remain (the upper bound is the stop condition)

#### Scenario: Input already within target range
- **WHEN** input contains 8 paragraph clusters and the target range is 6 to 20
- **THEN** the system returns all 8 clusters unchanged without merging

#### Scenario: Input below minimum target
- **WHEN** input contains 3 paragraph clusters and the target range minimum is 6
- **THEN** the system returns all 3 clusters unchanged (does not attempt to split)

### Requirement: AABB Distance Calculation
The system SHALL calculate the distance between two polygons as the Euclidean distance of their edge-to-edge gaps on the X and Y axes.

The system SHALL apply a configurable vertical weight multiplier to the Y-axis distance component. The default weight SHALL be 2.0.

The system SHALL treat overlapping projections (negative gap) as zero distance on that axis.

#### Scenario: Non-overlapping boxes on same row
- **WHEN** box A is at (0,0)-(10,10) and box B is at (15,0)-(25,10) with vertical weight 2.0
- **THEN** the distance is sqrt(5² + (0×2)²) = 5.0

#### Scenario: Vertically stacked boxes
- **WHEN** box A is at (0,0)-(10,10) and box B is at (0,15)-(10,25) with vertical weight 2.0
- **THEN** the distance is sqrt(0² + (5×2)²) = 10.0

#### Scenario: Overlapping projections
- **WHEN** box A is at (0,0)-(10,10) and box B is at (5,12)-(15,22)
- **THEN** X-axis projections overlap (5 < 10), so xDist = 0; yDist = 2; distance = sqrt(0² + (2×2)²) = 4.0

### Requirement: Polygon Merging
The system SHALL merge two or more polygons into a single bounding polygon that is the axis-aligned bounding box encompassing all input polygons.

#### Scenario: Merge two polygons
- **WHEN** polygon A is (0,0)-(10,10) and polygon B is (5,5)-(15,15)
- **THEN** the merged polygon is (0,0)-(15,15)

#### Scenario: Merge multiple polygons from a cluster
- **WHEN** a cluster contains three polygons: (0,0)-(10,10), (20,5)-(30,15), and (5,20)-(15,30)
- **THEN** the merged polygon is (0,0)-(30,30)

### Requirement: Content Ordering After Merge
When merging multiple paragraphs into one, the system SHALL concatenate their content strings in reading order: sorted by minimum Y (top-to-bottom), then by minimum X (left-to-right).

The system SHALL join content strings with a single space separator.

#### Scenario: Merge three paragraphs in reading order
- **WHEN** merging paragraphs: {content:"A", bbox:(10,20)-(20,30)}, {content:"B", bbox:(5,10)-(15,20)}, {content:"C", bbox:(30,10)-(40,20)}
- **THEN** the merged content is "B C A" (B and C share Y=10, sorted by X; A has Y=20, comes after)

### Requirement: Per-Page Processing
The system SHALL group input paragraphs by their Page property and process each page independently.

Paragraphs from different pages SHALL NOT be merged together.

#### Scenario: Multiple pages processed independently
- **WHEN** input contains 30 paragraphs on page 1 and 20 paragraphs on page 2, with target count 6–20
- **THEN** the system processes each page separately, producing merged paragraphs for page 1 and page 2 independently

### Requirement: Locked Cluster Support
The system SHALL accept pre-locked clusters that must not be split or merged with each other during HAC.

The HAC loop SHALL treat locked clusters as atomic units and SHALL NOT attempt to merge two locked clusters together.

Locked clusters MAY still be merged with non-locked (free) clusters during HAC.

#### Scenario: Locked table zone merged with nearby body text
- **WHEN** a locked table zone cluster and an adjacent free body-text cluster are the two closest clusters
- **THEN** the HAC loop merges them into one cluster (the table zone stays intact)

#### Scenario: Two locked clusters are closest
- **WHEN** two locked table zone clusters are the two closest clusters
- **THEN** the HAC loop skips this pair and selects the next-closest pair for merging
