## Why

The current `MinParagraphsPerPage` / `MaxParagraphsPerPage` range API is misleading — only the maximum is used by the HAC algorithm. `MinParagraphsPerPage` is validated but never consulted during merging, creating a false impression that the library targets a range. In practice, the consumer just wants "at most N paragraphs per page." Additionally, on sparse pages, the HAC can merge paragraphs that are arbitrarily far apart just to hit the target count, producing absurd merged segments.

## What Changes

- **BREAKING:** Replace `MinParagraphsPerPage` and `MaxParagraphsPerPage` with a single `TargetParagraphsPerPage` (int, default 20).
- Add `MaxMergeDistance` (double?, default null/disabled): Optional safety valve — even if above the target count, never merge two clusters whose AABB distance exceeds this threshold. Prevents absurd merges on sparse pages.
- **BREAKING:** Remove `MinParagraphsPerPage` validation (no longer exists).
- HAC loop now stops when count ≤ `TargetParagraphsPerPage` OR when the closest pair exceeds `MaxMergeDistance` (if set).
- Rename all internal references from "min/max" range to single "target".

## Capabilities

### Modified Capabilities

- `polygon-merge-config`: Replace `MinParagraphsPerPage` and `MaxParagraphsPerPage` with `TargetParagraphsPerPage`. Add optional `MaxMergeDistance`. Update options validation.
- `spatial-clustering`: HAC stop condition changes from ceiling-only to ceiling + optional distance threshold. Scenarios updated to reflect single target count.

## Impact

- `ParagraphGrouperOptions.cs`: Remove two properties, add two, update validation.
- `ParagraphGrouper.cs`: `ReduceClusters` signature and logic updated.
- All tests referencing `MinParagraphsPerPage` / `MaxParagraphsPerPage` need updating.
- README.md configuration table needs updating.
- **BREAKING** for all consumers — migration: rename `MaxParagraphsPerPage` → `TargetParagraphsPerPage`, remove `MinParagraphsPerPage`.
