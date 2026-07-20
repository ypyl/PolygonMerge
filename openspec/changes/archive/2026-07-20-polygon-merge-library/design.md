## Context

Azure Document Intelligence (and similar OCR services) parse PDFs into structured JSON with per-paragraph entries containing page number, text content, and bounding polygon. A single page can contain 50–200+ such fragments — each line, word, or table cell is a separate entry. Downstream consumers (RAG pipelines, summarization, content extraction) need larger, semantically coherent paragraphs, not atomic line-level fragments.

Document Intelligence also provides table structure metadata: which paragraphs belong to which table cells, identified by row and column indices. This is a first-class input to the library — when available, table rows are merged deterministically before spatial clustering.

The core challenge is spatial: paragraphs that are visually close should merge. The complication is tables — grid cells that share row/column alignment. Standard proximity-based clustering can merge table cells horizontally before vertically, scrambling reading order. Paragraph polygons may or may not overlap depending on the OCR engine; the library must handle both cases.

## Goals / Non-Goals

**Goals:**
- Provide a .NET library that accepts a list of OCR paragraph DTOs and returns merged paragraphs grouped by page.
- Reduce paragraph count per page to a configurable target range (default 6–20).
- Detect and preserve table structures — table cells must stay in the same merged segment.
- Zero external dependencies beyond the .NET BCL.
- Clean, testable API suitable for DI registration.
- NuGet-packaged for easy consumption.

**Non-Goals:**
- Reading raw PDF files — the library operates on already-parsed OCR JSON.
- OCR text correction or content analysis — purely spatial/geometric merging.
- Multi-column layout detection beyond table adjacency — complex multi-column body text merging is deferred.
- Real-time performance optimization — correctness and clarity first; acceptable for batch processing of typical documents (hundreds of pages).
- Any ML-based or trained model approach — pure algorithmic.

## Decisions

### Decision 1: Two-Pass Algorithm (Table Pre-Pass + Hierarchical Agglomerative Clustering)

**Choice:** Run a table-detection pre-pass that collapses grid-aligned cells into locked "table zone" clusters, then run hierarchical agglomerative clustering (HAC) on the remaining blocks to reach the target paragraph count.

**Alternatives considered:**
- **Pure HAC only (Option 1 from research):** Merges closest blocks iteratively. Simple but can scramble table cells — a cell in column A close to a cell in column B merges horizontally before merging vertically down its own column, destroying table reading order.
- **Recursive XY-Cut (Option 2 / PdfPig):** Splits page by whitespace gaps recursively. Handles multi-column body text well but fragments tables because whitespace between columns acts as a cut line, splitting table rows apart.
- **DBSCAN:** Density-based clustering. Handles tables well but requires careful epsilon tuning and doesn't give deterministic control over final paragraph count.

**Rationale:** The two-pass approach gives the best of both worlds: deterministic HAC control over final paragraph count, with a pre-pass that locks table cells into indivisible units. No external library dependency.

### Decision 2: Explicit Table Structure as Primary Path

**Choice:** Accept an optional list of `TableCellInfo` records (`{Row, Column, ParagraphId}`) from the consumer. When provided, the library merges all cells within the same table row into a single locked cluster — no heuristic guessing needed.

**Rationale:** The consumer (or upstream OCR pipeline) often already knows which paragraphs belong to which table cells. Passing this information explicitly is more reliable than spatial heuristics. Row-level merging makes sense because cells in a table row logically belong together as one line of tabular content.

**Merge strategy:** All cells with the same `(TableId, Row)` are merged into one locked cluster before HAC. Cells in different rows within the same table remain separate clusters (but all locked), so HAC won't merge rows with adjacent body text unless closest.

### Decision 3: Grid Adjacency as Heuristic Fallback

**Choice:** When explicit `TableCellInfo` data is not provided, fall back to grid-adjacency heuristic detection based on edge-to-edge alignment and gap thresholds.

**Rationale:** Table cells share horizontal or vertical alignment projections — row-mates have overlapping Y-ranges with a small X-gap between them; column-mates have overlapping X-ranges with a small Y-gap. This holds regardless of whether polygons overlap: overlapping cells have gap=0 on both axes, which still satisfies the neighbor criteria. The heuristic checks: (a) Y-ranges overlap AND X-gap ≤ MaxHorizontalGap → same row; (b) X-ranges overlap AND Y-gap ≤ MaxVerticalGap → same column.

**Configurable thresholds:** `MaxHorizontalGap` (default 25pt) and `MaxVerticalGap` (default 15pt) for table neighbor detection. These sane defaults work for typical 10–12pt font tables but are overridable.

**Note:** The heuristic mode merges the entire grid into one locked zone (transitive closure). This differs from explicit mode where each row stays separate. The heuristic assumes tight tables where the whole table should be one segment; if per-row separation is needed, the consumer should use explicit mode.

### Decision 4: AABB Distance with Vertical Weighting for HAC

**Choice:** Axis-aligned bounding box distance with configurable vertical weighting (default 2× on Y-axis).

**Rationale:** Reading order flows top-to-bottom, left-to-right. A 2× vertical weight means blocks stacked vertically are seen as "closer" than blocks side-by-side at the same absolute distance, producing more natural paragraph merging. The weight is configurable.

**Formula:** `sqrt(xDist² + (yDist × weight)²)` where xDist/yDist are edge-to-edge gaps (0 if overlapping on that axis).

### Decision 5: Target .NET 10

**Choice:** Target `net10.0` only.

**Rationale:** .NET 10 is the current LTS (released November 2025). Single-target keeps the project simple — no multi-targeting build matrix, no API compatibility shims. Consumers are expected to be on modern .NET.

### Decision 6: No External Dependencies

**Choice:** Zero NuGet dependencies beyond the BCL.

**Rationale:** The algorithms are straightforward geometry. Adding ML.NET or PdfPig would add megabytes of dependencies for what amounts to ~300 lines of spatial math. The user asked for a lightweight library, not a framework.

### Decision 7: Per-Page Processing Model

**Choice:** The library groups input by `Page` property and processes each page independently. Paragraphs from different pages MUST NOT be merged under any circumstances.

**Rationale:** Each paragraph has a `Page` value from the OCR engine. A paragraph belongs to exactly one page. Merging across pages would break page boundaries and produce incorrect output. Per-page processing is also simpler and parallelizable.

## Risks / Trade-offs

- **[Risk] Table detection false positives:** Body text with coincidental grid-like alignment could be locked as a "table zone." → **Mitigation:** Conservative default gap thresholds (25pt/15pt). Users can tighten thresholds for dense layouts. False positives are less harmful than false negatives (tables split apart).
- **[Risk] Table detection false negatives:** Wide-gapped tables (e.g., financial statements with large column spacing) may not be detected. → **Mitigation:** Configurable thresholds. Users can increase `MaxHorizontalGap` for spread-out tables.
- **[Risk] No reading-order awareness beyond Y-weighting:** The HAC distance function knows about vertical vs horizontal proximity but doesn't understand reading order within a merged paragraph — ordering is done by top-to-bottom, left-to-right sort after merge. This works for standard layouts but may misorder complex multi-column body text. → **Mitigation:** Documented as a non-goal. Users with multi-column layouts can pre-process or post-process.
- **[Risk] Performance on large documents:** O(n²) pairwise distance checks in HAC loop. For 200 paragraphs per page × 50 pages = manageable. For 10,000+ paragraphs total, may need optimization. → **Mitigation:** First version targets correctness. Performance optimization (spatial indexing, k-d trees) deferred to future iterations.
