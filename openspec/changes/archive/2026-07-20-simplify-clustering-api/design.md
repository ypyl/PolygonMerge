## Context

The current options use `MinParagraphsPerPage` (6) and `MaxParagraphsPerPage` (20) implying a range target. However, `ReduceClusters` only consults `MaxParagraphsPerPage`. `MinParagraphsPerPage` is dead configuration that confuses users. Additionally, without a distance safety valve, sparse pages with distant paragraphs get merged into nonsensical segments just to reach the target count.

## Goals / Non-Goals

**Goals:**
- Single, honest configuration: `TargetParagraphsPerPage` — "I want at most this many per page."
- Optional `MaxMergeDistance` to prevent over-merging on sparse layouts.
- Clean breaking change — no backwards compatibility layer.

**Non-Goals:**
- Splitting paragraphs to reach a minimum count (NLP territory).
- Per-page target customization (same target for all pages).
- Dynamic distance thresholds based on page density.

## Decisions

### Decision 1: Single Target, Not Range

**Choice:** `TargetParagraphsPerPage` (int, default 20). HAC stops when cluster count ≤ target.

**Rationale:** The original Min/Max range was aspirational. The lower bound can't be enforced without splitting. A single ceiling is honest about what the algorithm does. Consumers who want a tighter range simply set `TargetParagraphsPerPage` to the desired upper bound.

### Decision 2: Optional MaxMergeDistance

**Choice:** `MaxMergeDistance` (double?, default null). When set, the HAC loop's pairwise search still finds the closest pair, but if that pair's distance exceeds the threshold, the loop stops early. This is evaluated per-iteration (the closest pair might be acceptable, the next might not).

**Rationale:** On a page with 8 paragraphs spread across the entire page height, the HAC would normally merge them down to 1 if `TargetParagraphsPerPage` were 1. With `MaxMergeDistance = 50`, merges stop once the closest pair is >50pt apart, preserving natural paragraph boundaries.

**Behavior when both constraints active:** HAC stops when EITHER `count ≤ TargetParagraphsPerPage` OR `closest distance > MaxMergeDistance`.

### Decision 3: Keep Default at 20, Not Lower

**Choice:** Default `TargetParagraphsPerPage` remains 20 (same as old `MaxParagraphsPerPage`).

**Rationale:** The existing default works well for typical dense OCR pages. Lowering it would be a behavioral change on top of the API change.

### Decision 4: MaxMergeDistance Does Not Apply to Table Detection

**Choice:** `MaxMergeDistance` only gates the HAC merge loop, not the heuristic table detection pre-pass.

**Rationale:** Table detection uses its own gap thresholds (`MaxTableHorizontalGap`, `MaxTableVerticalGap`) which are specifically tuned for table cell adjacency. Mixing the general-purpose distance cap into table detection would make tuning harder.

## Risks / Trade-offs

- **[Risk] Breaking change for existing consumers.** → **Mitigation:** Migration is a simple rename: `MaxParagraphsPerPage` → `TargetParagraphsPerPage`, remove `MinParagraphsPerPage`. Documented in the NuGet release notes.
- **[Risk] `MaxMergeDistance` defaults to null (off).** Consumers who would benefit from it might not discover it. → **Mitigation:** Prominent in README and XML docs.
