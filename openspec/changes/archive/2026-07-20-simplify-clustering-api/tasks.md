## 1. Options API Change

- [x] 1.1 Rename `MaxParagraphsPerPage` → `TargetParagraphsPerPage` in `ParagraphGrouperOptions`
- [x] 1.2 Remove `MinParagraphsPerPage` property
- [x] 1.3 Add `MaxMergeDistance` property (double?, default null)
- [x] 1.4 Update `Validate()` to check `TargetParagraphsPerPage ≥ 1`, `MaxMergeDistance ≥ 0` if set, remove old Min/Max validation

## 2. HAC Algorithm Update

- [x] 2.1 Update `ReduceClusters` to use `TargetParagraphsPerPage` instead of `MaxParagraphsPerPage`
- [x] 2.2 Add `MaxMergeDistance` early-exit: if closest pair distance > threshold, break out of the merge loop

## 3. Tests

- [x] 3.1 Update all existing tests to use `TargetParagraphsPerPage` instead of `MinParagraphsPerPage`/`MaxParagraphsPerPage`
- [x] 3.2 Add test: HAC stops early when closest pair exceeds `MaxMergeDistance`
- [x] 3.3 Add test: `MaxMergeDistance` = null means no distance cap (existing behavior)
- [x] 3.4 Add test: options validation for `TargetParagraphsPerPage < 1` and negative `MaxMergeDistance`

## 4. Documentation

- [x] 4.1 Update README.md configuration table to reflect new option names
- [x] 4.2 Update XML doc comments on `ParagraphGrouperOptions` properties
