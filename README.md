# PolygonMerge

Lightweight .NET library for merging fragmented OCR paragraphs into coherent segments using spatial clustering with table structure preservation.

Designed for post-processing Azure Document Intelligence (and similar OCR services) output — takes hundreds of tiny paragraph fragments and groups them into 6–20 coherent paragraphs per page.

## Installation

```bash
dotnet add package PolygonMerge
```

## Quick Start

### Basic Usage (Spatial Clustering Only)

```csharp
using PolygonMerge;

var paragraphs = new List<OcrParagraph>
{
    new() { Page = 1, Content = "Hello", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 10 } },
    new() { Page = 1, Content = "World", BoundingBox = new Polygon { MinX = 0, MinY = 12, MaxX = 50, MaxY = 22 } },
    // ... more paragraphs
};

var grouper = new ParagraphGrouper(new ParagraphGrouperOptions
{
    MaxParagraphsPerPage = 12
});

var merged = grouper.Group(paragraphs);
// merged.Count <= 12
```

### With Table Structure (Document Intelligence)

```csharp
var paragraphs = new List<OcrParagraph> { /* ... */ };

var tableCells = new List<TableCellInfo>
{
    new() { ParagraphId = "p1", Row = 0, Column = 0, TableId = "table1" },
    new() { ParagraphId = "p2", Row = 0, Column = 1, TableId = "table1" },
    new() { ParagraphId = "p3", Row = 1, Column = 0, TableId = "table1" },
    new() { ParagraphId = "p4", Row = 1, Column = 1, TableId = "table1" },
};

var grouper = new ParagraphGrouper(new ParagraphGrouperOptions());

// Cells in the same row are merged into locked clusters before spatial clustering
var merged = grouper.Group(paragraphs, tableCells, p => p.Id);
```

### Dependency Injection

```csharp
services.AddParagraphGrouper(options =>
{
    options.TargetParagraphsPerPage = 10;
    options.EnableTableDetection = true;
});
```

## How It Works

1. **Table Pre-Pass**: If explicit table structure (`TableCellInfo`) is provided, cells in the same row are merged into locked clusters. Otherwise, a heuristic grid-adjacency detector finds and locks table zones.

2. **Hierarchical Agglomerative Clustering**: Iteratively merges the two closest clusters (by weighted AABB distance) until the target paragraph count per page is reached. Locked clusters are never merged with other locked clusters.

3. **Per-Page Processing**: Each page is processed independently — paragraphs from different pages are never merged.

## Configuration

| Option | Default | Description |
|--------|---------|-------------|
| `TargetParagraphsPerPage` | 20 | Maximum target paragraphs per page (HAC stop condition) |
| `MaxMergeDistance` | null | Optional distance cap — stops HAC early if closest pair exceeds this (points) |
| `VerticalDistanceWeight` | 2.0 | Y-axis weight multiplier for reading-order-aware proximity |
| `MaxTableHorizontalGap` | 25.0 | Max X-gap (points) for heuristic table row detection |
| `MaxTableVerticalGap` | 15.0 | Max Y-gap (points) for heuristic table column detection |
| `EnableTableDetection` | true | Run heuristic table detection when no explicit structure provided |

## License

MIT
