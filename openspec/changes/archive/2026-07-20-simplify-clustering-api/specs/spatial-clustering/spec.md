## MODIFIED Requirements

### Requirement: Hierarchical Agglomerative Clustering
The system SHALL merge a list of paragraph clusters into a target number of clusters using hierarchical agglomerative clustering with axis-aligned bounding box (AABB) distance.

The algorithm SHALL iteratively find the two closest clusters, merge them, and repeat until:
- The cluster count reaches or falls below `TargetParagraphsPerPage`, OR
- The distance between the closest pair exceeds `MaxMergeDistance` (if configured).

When `MaxMergeDistance` is null (not configured), only the count-based stop condition applies.

#### Scenario: Reduce clusters to target count
- **WHEN** input contains 50 paragraph clusters and TargetParagraphsPerPage is 20
- **THEN** the system merges clusters iteratively until 20 clusters remain

#### Scenario: Input already within target range
- **WHEN** input contains 8 paragraph clusters and TargetParagraphsPerPage is 20
- **THEN** the system returns all 8 clusters unchanged without merging

#### Scenario: Input below minimum target
- **WHEN** input contains 3 paragraph clusters and TargetParagraphsPerPage is 20
- **THEN** the system returns all 3 clusters unchanged (does not attempt to split)

#### Scenario: MaxMergeDistance stops early
- **WHEN** input contains 30 clusters, TargetParagraphsPerPage is 10, MaxMergeDistance is 50, and after 12 merges the closest remaining pair is 60pt apart
- **THEN** the HAC loop stops with 18 clusters (above the target but all remaining pairs exceed the distance cap)

#### Scenario: MaxMergeDistance not configured
- **WHEN** MaxMergeDistance is null and input contains 50 clusters with TargetParagraphsPerPage of 20
- **THEN** the system merges down to 20 clusters regardless of distance (no distance cap)

## ADDED Requirements

### Requirement: MaxMergeDistance Does Not Apply to Locked Clusters
When `MaxMergeDistance` is set, the distance check SHALL still respect the existing locked-cluster constraint: if two locked clusters are the closest pair, they are skipped regardless of distance.

Locked clusters MAY still be merged with free clusters if their distance is within the cap.

#### Scenario: Locked clusters skipped even within distance cap
- **WHEN** two locked clusters are 10pt apart (within MaxMergeDistance=50) but both are locked
- **THEN** the pair is skipped, and the next-closest eligible pair is considered

#### Scenario: Locked cluster merges with free cluster within cap
- **WHEN** a locked cluster and a free cluster are 15pt apart (within MaxMergeDistance=50)
- **THEN** they are merged as normal
