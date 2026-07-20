## Why

Azure Document Intelligence (and similar OCR services) parses PDFs into structured JSON, but produces hundreds of fragmented paragraphs per page — each line, word, or table cell becomes its own bounding box with a few words of content. Downstream processing (summarization, chunking for RAG, content extraction) needs coherent, larger text blocks — roughly 6–20 per page — not atomic fragments. Existing .NET PDF libraries (PdfPig, iText) are designed to parse raw PDFs, not post-process OCR JSON output. A lightweight, focused .NET library for spatially clustering Document Intelligence output into larger paragraphs fills this gap, with table structure preservation as a first-class concern.

## What Changes

- New .NET class library `PolygonMerge` that takes Document Intelligence OCR JSON (page, content, polygon) and outputs merged paragraphs grouped by page.
- Two-pass clustering algorithm: a table-aware pre-pass that locks table cells into unified blocks (cells within the same row are merged), followed by hierarchical agglomerative clustering to reduce paragraph count to a target range.
- Accepts explicit table structure metadata (list of table cells with row, column, paragraph ID) so table paragraphs are merged by row before general clustering.
- Heuristic grid-adjacency table detection as a fallback when explicit table metadata is not provided.
- Configurable target paragraph count per page (default 6–20).
- Configurable gap thresholds for heuristic table detection (horizontal and vertical gaps).
- Bounding box distance utilities (axis-aligned bounding box distance with vertical reading-flow weighting).
- Polygon merge utilities (union of bounding boxes).
- NuGet-packaged for easy consumption in .NET projects.

## Capabilities

### New Capabilities

- `spatial-clustering`: Core hierarchical agglomerative clustering engine that merges the closest bounding boxes iteratively until the desired paragraph count is reached. Uses AABB distance with configurable vertical weighting for reading-order awareness.
- `table-structure`: Accepts explicit table cell metadata (row, column, paragraph ID) and merges paragraphs within the same table row into locked clusters before general clustering. This is the primary table-preservation path.
- `table-detection`: Heuristic grid-adjacency pre-pass that identifies non-overlapping table cells by checking horizontal/vertical alignment and gap thresholds. Serves as a fallback when explicit table structure metadata is not available.
- `ocr-model`: Input/output model classes for OCR paragraphs (page, content, bounding box polygon) and a JSON serialization contract.
- `polygon-merge-config`: Configuration model exposing target paragraph range, table gap thresholds, and distance weighting parameters.

### Modified Capabilities

<!-- No existing capabilities to modify — this is a new library. -->

## Impact

- New NuGet package: `PolygonMerge` targeting .NET 10.
- New source project under `/src/PolygonMerge/` with class library, unit test project, and solution file.
- No breaking changes — greenfield library.
- Dependencies: none beyond `Microsoft.Extensions.DependencyInjection` (optional DI support) and standard .NET BCL. No external ML or PDF processing libraries required.
