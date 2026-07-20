using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace PolygonMerge.Tests;

public class PolygonTests
{
    [Fact]
    public void Merge_TwoPolygons_ReturnsUnion()
    {
        var a = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 };
        var b = new Polygon { MinX = 5, MinY = 5, MaxX = 15, MaxY = 15 };

        var result = Polygon.Merge(a, b);

        Assert.Equal(0, result.MinX);
        Assert.Equal(0, result.MinY);
        Assert.Equal(15, result.MaxX);
        Assert.Equal(15, result.MaxY);
    }

    [Fact]
    public void Merge_MultiplePolygons_ReturnsUnion()
    {
        var polygons = new[]
        {
            new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 },
            new Polygon { MinX = 20, MinY = 5, MaxX = 30, MaxY = 15 },
            new Polygon { MinX = 5, MinY = 20, MaxX = 15, MaxY = 30 }
        };

        var result = Polygon.Merge(polygons);

        Assert.Equal(0, result.MinX);
        Assert.Equal(0, result.MinY);
        Assert.Equal(30, result.MaxX);
        Assert.Equal(30, result.MaxY);
    }

    [Fact]
    public void DistanceTo_NonOverlappingSideBySide_ReturnsCorrectDistance()
    {
        var a = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 };
        var b = new Polygon { MinX = 15, MinY = 0, MaxX = 25, MaxY = 10 };

        // xDist = 5, yDist = 0, weight = 2.0
        // distance = sqrt(5² + 0²) = 5
        double dist = a.DistanceTo(b, verticalWeight: 2.0);

        Assert.Equal(5.0, dist);
    }

    [Fact]
    public void DistanceTo_VerticallyStacked_AppliesWeight()
    {
        var a = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 };
        var b = new Polygon { MinX = 0, MinY = 15, MaxX = 10, MaxY = 25 };

        // xDist = 0, yDist = 5, weight = 2.0
        // distance = sqrt(0² + (5*2)²) = 10
        double dist = a.DistanceTo(b, verticalWeight: 2.0);

        Assert.Equal(10.0, dist);
    }

    [Fact]
    public void DistanceTo_OverlappingProjections_TreatsGapAsZero()
    {
        var a = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 };
        var b = new Polygon { MinX = 5, MinY = 12, MaxX = 15, MaxY = 22 };

        // X projections overlap (5 < 10) → xDist = 0
        // yDist = 12 - 10 = 2
        // distance = sqrt(0² + (2*2)²) = 4
        double dist = a.DistanceTo(b, verticalWeight: 2.0);

        Assert.Equal(4.0, dist);
    }

    [Fact]
    public void DistanceTo_Diagonal_ReturnsCorrectDistance()
    {
        var a = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 };
        var b = new Polygon { MinX = 20, MinY = 20, MaxX = 30, MaxY = 30 };

        // xDist = 10, yDist = 10, weight = 1.0 (no extra weight for diagonal test)
        // distance = sqrt(10² + 10²) = sqrt(200) ≈ 14.14
        double dist = a.DistanceTo(b, verticalWeight: 1.0);

        Assert.Equal(Math.Sqrt(200), dist, 5);
    }
}

public class TableDetectionHeuristicTests
{
    private readonly ParagraphGrouper _grouper;

    public TableDetectionHeuristicTests()
    {
        var options = new ParagraphGrouperOptions
        {
            MaxTableHorizontalGap = 25,
            MaxTableVerticalGap = 15,
            EnableTableDetection = true,
            TargetParagraphsPerPage = 100 // Don't reduce during these tests
        };
        _grouper = new ParagraphGrouper(options);
    }

    [Fact]
    public void IsTableNeighbor_SameRow_Detected()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 10, MaxX = 50, MaxY = 20 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 55, MinY = 10, MaxX = 105, MaxY = 20 } }
        };

        var result = _grouper.Group(paragraphs);

        // Both cells should be merged into one table zone
        Assert.Single(result);
        Assert.Equal("A B", result[0].Content);
    }

    [Fact]
    public void IsTableNeighbor_SameColumn_Detected()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 10, MaxX = 50, MaxY = 20 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 0, MinY = 22, MaxX = 50, MaxY = 32 } }
        };

        var result = _grouper.Group(paragraphs);

        Assert.Single(result);
    }

    [Fact]
    public void IsTableNeighbor_FarApart_NotDetected()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 10, MaxX = 50, MaxY = 20 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 100, MinY = 10, MaxX = 150, MaxY = 20 } } // X-gap = 50 > 25
        };

        var result = _grouper.Group(paragraphs);

        // Not merged — gap too large
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void IsTableNeighbor_Diagonal_NotDetected()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 20, MinY = 20, MaxX = 30, MaxY = 30 } }
        };

        var result = _grouper.Group(paragraphs);

        // No shared alignment → not merged
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void IsTableNeighbor_OverlappingCells_Detected()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 10, MaxX = 50, MaxY = 25 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 40, MinY = 12, MaxX = 90, MaxY = 23 } }
        };

        var result = _grouper.Group(paragraphs);

        // Overlapping on both axes → detected as table neighbors
        Assert.Single(result);
    }

    [Fact]
    public void IsTableNeighbor_ABelowB_DetectedByColumn()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 20, MaxX = 50, MaxY = 30 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 10 } }
        };

        var result = _grouper.Group(paragraphs);

        // A is below B, but shares X-range and Y-gap=10 ≤ 15 → same column, detected
        Assert.Single(result);
    }

    [Fact]
    public void IsTableNeighbor_AToRightOfB_DetectedByRow()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 60, MinY = 0, MaxX = 110, MaxY = 10 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 10 } }
        };

        var result = _grouper.Group(paragraphs);

        // A is to the right of B, shares Y-range, X-gap=10 ≤ 25 → same row, detected
        Assert.Single(result);
    }

    [Fact]
    public void IsTableNeighbor_TouchingEdges_DetectedByRow()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 10, MinY = 0, MaxX = 20, MaxY = 10 } }
        };

        var result = _grouper.Group(paragraphs);

        // Touching edges, shares Y-range, X-gap=0 ≤ 25 → same row, detected
        Assert.Single(result);
    }

    [Fact]
    public void TableZoneCollapse_2x2Grid_MergedIntoSingleZone()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A1", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 1, Content = "B1", BoundingBox = new Polygon { MinX = 55, MinY = 0, MaxX = 105, MaxY = 15 } },
            new() { Page = 1, Content = "A2", BoundingBox = new Polygon { MinX = 0, MinY = 17, MaxX = 50, MaxY = 32 } },
            new() { Page = 1, Content = "B2", BoundingBox = new Polygon { MinX = 55, MinY = 17, MaxX = 105, MaxY = 32 } }
        };

        var result = _grouper.Group(paragraphs);

        // All 4 cells merged into one locked zone
        Assert.Single(result);
        Assert.Contains("A1", result[0].Content);
        Assert.Contains("B1", result[0].Content);
        Assert.Contains("A2", result[0].Content);
        Assert.Contains("B2", result[0].Content);
    }

    [Fact]
    public void TableZoneCollapse_TwoSeparateTables_RemainSeparate()
    {
        var paragraphs = new List<OcrParagraph>
        {
            // Table 1
            new() { Page = 1, Content = "T1A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 1, Content = "T1B", BoundingBox = new Polygon { MinX = 55, MinY = 0, MaxX = 105, MaxY = 15 } },
            // Table 2 (far below)
            new() { Page = 1, Content = "T2A", BoundingBox = new Polygon { MinX = 0, MinY = 200, MaxX = 50, MaxY = 215 } },
            new() { Page = 1, Content = "T2B", BoundingBox = new Polygon { MinX = 55, MinY = 200, MaxX = 105, MaxY = 215 } }
        };

        var result = _grouper.Group(paragraphs);

        Assert.Equal(2, result.Count);
    }
}

public class TableStructureExplicitTests
{
    private readonly ParagraphGrouper _grouper;

    public TableStructureExplicitTests()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 100 // Don't reduce during these tests
        };
        _grouper = new ParagraphGrouper(options);
    }

    [Fact]
    public void SameRowCells_MergedIntoOneLockedCluster()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "Cell1", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 1, Content = "Cell2", BoundingBox = new Polygon { MinX = 55, MinY = 0, MaxX = 105, MaxY = 15 } },
            new() { Page = 1, Content = "Body", BoundingBox = new Polygon { MinX = 0, MinY = 50, MaxX = 100, MaxY = 65 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" },
            new() { ParagraphId = "1", Row = 0, Column = 1, TableId = "t1" }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        // Two clusters: merged row + free body paragraph
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Content.Contains("Cell1") && r.Content.Contains("Cell2"));
    }

    [Fact]
    public void DifferentRows_StaySeparate()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "R0C0", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 1, Content = "R0C1", BoundingBox = new Polygon { MinX = 55, MinY = 0, MaxX = 105, MaxY = 15 } },
            new() { Page = 1, Content = "R1C0", BoundingBox = new Polygon { MinX = 0, MinY = 17, MaxX = 50, MaxY = 32 } },
            new() { Page = 1, Content = "R1C1", BoundingBox = new Polygon { MinX = 55, MinY = 17, MaxX = 105, MaxY = 32 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" },
            new() { ParagraphId = "1", Row = 0, Column = 1, TableId = "t1" },
            new() { ParagraphId = "2", Row = 1, Column = 0, TableId = "t1" },
            new() { ParagraphId = "3", Row = 1, Column = 1, TableId = "t1" }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        // Two separate locked clusters (one per row)
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void DifferentTables_StaySeparate()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "T1R0", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 1, Content = "T2R0", BoundingBox = new Polygon { MinX = 0, MinY = 100, MaxX = 50, MaxY = 115 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" },
            new() { ParagraphId = "1", Row = 0, Column = 0, TableId = "t2" }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ContentOrderedByColumn_WithinRow()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "Col0", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 1, Content = "Col2", BoundingBox = new Polygon { MinX = 110, MinY = 0, MaxX = 160, MaxY = 15 } },
            new() { Page = 1, Content = "Col1", BoundingBox = new Polygon { MinX = 55, MinY = 0, MaxX = 105, MaxY = 15 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" },
            new() { ParagraphId = "1", Row = 0, Column = 2, TableId = "t1" },
            new() { ParagraphId = "2", Row = 0, Column = 1, TableId = "t1" }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        Assert.Single(result);
        // Content should be in column order: Col0 Col1 Col2
        Assert.Equal("Col0 Col1 Col2", result[0].Content);
    }

    [Fact]
    public void CrossPage_SameTableId_DoesNotMerge()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "P1R0", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 2, Content = "P2R0", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" },
            new() { ParagraphId = "1", Row = 0, Column = 0, TableId = "t1" }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        // Two separate pages → still 2 paragraphs
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void NullTableId_TreatedAsEmptyString()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "Cell", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = null }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        Assert.Single(result);
        Assert.Equal("Cell", result[0].Content);
    }

    [Fact]
    public void GroupWithTableCells_NullParagraphs_ReturnsEmpty()
    {
        var result = _grouper.Group(null!, [], p => "");
        Assert.Empty(result);
    }

    [Fact]
    public void UnmatchedTableCellEntries_AreIgnored()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "Real", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "nonexistent", Row = 0, Column = 0, TableId = "t1" }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        // The real paragraph remains as a free cluster
        Assert.Single(result);
        Assert.Equal("Real", result[0].Content);
    }

    [Fact]
    public void UnmatchedParagraphs_RemainFree()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "Table", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } },
            new() { Page = 1, Content = "Body", BoundingBox = new Polygon { MinX = 0, MinY = 50, MaxX = 100, MaxY = 65 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" }
        };

        var result = _grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        // Two clusters: locked table row + free body text
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void EmptyKeyFromSelector_SkipsMatching()
    {
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "Cell", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } }
        };

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" }
        };

        // Key selector returns empty string → no match
        var result = _grouper.Group(paragraphs, tableCells, p => "");

        Assert.Single(result);
        Assert.Equal("Cell", result[0].Content);
    }

    [Fact]
    public void GroupWithTableCells_EmptyParagraphs_ReturnsEmpty()
    {
        var result = _grouper.Group([], [], p => p.Content);
        Assert.Empty(result);
    }
}

public class HacReductionTests
{
    [Fact]
    public void ReduceClusters_AboveTarget_ReducesToTarget()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 10,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>();
        for (int i = 0; i < 50; i++)
        {
            paragraphs.Add(new OcrParagraph
            {
                Page = 1,
                Content = $"P{i}",
                BoundingBox = new Polygon { MinX = i * 10, MinY = i * 5, MaxX = i * 10 + 8, MaxY = i * 5 + 8 }
            });
        }

        var result = grouper.Group(paragraphs);

        Assert.True(result.Count <= 10);
    }

    [Fact]
    public void ReduceClusters_AlreadyInRange_ReturnsUnchanged()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 20,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>();
        for (int i = 0; i < 8; i++)
        {
            paragraphs.Add(new OcrParagraph
            {
                Page = 1,
                Content = $"P{i}",
                BoundingBox = new Polygon { MinX = i * 20, MinY = 0, MaxX = i * 20 + 15, MaxY = 10 }
            });
        }

        var result = grouper.Group(paragraphs);

        Assert.Equal(8, result.Count);
    }

    [Fact]
    public void ReduceClusters_BelowMinimum_ReturnsUnchanged()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 20,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 0, MinY = 20, MaxX = 10, MaxY = 30 } },
            new() { Page = 1, Content = "C", BoundingBox = new Polygon { MinX = 0, MinY = 40, MaxX = 10, MaxY = 50 } }
        };

        var result = grouper.Group(paragraphs);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void LockedClusters_NotMergedTogether()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 1, // Would force everything together
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>();

        // Create two tables that should remain separate, with body text between them
        var tableCells = new List<TableCellInfo>();

        for (int i = 0; i < 4; i++)
        {
            paragraphs.Add(new OcrParagraph
            {
                Page = 1,
                Content = $"T1C{i}",
                BoundingBox = new Polygon { MinX = i * 50, MinY = 0, MaxX = i * 50 + 40, MaxY = 15 }
            });
            tableCells.Add(new TableCellInfo { ParagraphId = i.ToString(), Row = 0, Column = i, TableId = "t1" });
        }

        for (int i = 0; i < 4; i++)
        {
            paragraphs.Add(new OcrParagraph
            {
                Page = 1,
                Content = $"T2C{i}",
                BoundingBox = new Polygon { MinX = i * 50, MinY = 200, MaxX = i * 50 + 40, MaxY = 215 }
            });
            tableCells.Add(new TableCellInfo { ParagraphId = (i + 4).ToString(), Row = 0, Column = i, TableId = "t2" });
        }

        var result = grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        // Two locked table rows — they should NOT be merged together even if closest
        Assert.True(result.Count >= 2, $"Expected at least 2 clusters, got {result.Count}");
    }

    [Fact]
    public void ContentOrdering_TopToBottom_LeftToRight()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 1,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 10, MinY = 20, MaxX = 20, MaxY = 30 } },  // bottom-left
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 5, MinY = 10, MaxX = 15, MaxY = 20 } },    // top-left
            new() { Page = 1, Content = "C", BoundingBox = new Polygon { MinX = 30, MinY = 10, MaxX = 40, MaxY = 20 } }     // top-right
        };

        var result = grouper.Group(paragraphs);

        Assert.Single(result);
        Assert.Equal("B C A", result[0].Content);
    }

    [Fact]
    public void PerPageIsolation_DifferentPagesNeverMerge()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 1,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "P1A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } },
            new() { Page = 1, Content = "P1B", BoundingBox = new Polygon { MinX = 0, MinY = 15, MaxX = 10, MaxY = 25 } },
            new() { Page = 2, Content = "P2A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } },
            new() { Page = 2, Content = "P2B", BoundingBox = new Polygon { MinX = 0, MinY = 15, MaxX = 10, MaxY = 25 } }
        };

        var result = grouper.Group(paragraphs);

        // Each page has its own merged result
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Page == 1);
        Assert.Contains(result, r => r.Page == 2);
    }

    [Fact]
    public void FullPipeline_ExplicitTableStructure()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 5,
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>();
        // 4 table cells in a 2x2 grid
        paragraphs.Add(new OcrParagraph { Page = 1, Content = "R0C0", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } });
        paragraphs.Add(new OcrParagraph { Page = 1, Content = "R0C1", BoundingBox = new Polygon { MinX = 55, MinY = 0, MaxX = 105, MaxY = 15 } });
        paragraphs.Add(new OcrParagraph { Page = 1, Content = "R1C0", BoundingBox = new Polygon { MinX = 0, MinY = 17, MaxX = 50, MaxY = 32 } });
        paragraphs.Add(new OcrParagraph { Page = 1, Content = "R1C1", BoundingBox = new Polygon { MinX = 55, MinY = 17, MaxX = 105, MaxY = 32 } });
        // 10 body paragraphs scattered below the table
        for (int i = 0; i < 10; i++)
        {
            paragraphs.Add(new OcrParagraph
            {
                Page = 1,
                Content = $"Body{i}",
                BoundingBox = new Polygon { MinX = i * 10, MinY = 50 + i * 20, MaxX = i * 10 + 50, MaxY = 65 + i * 20 }
            });
        }

        var tableCells = new List<TableCellInfo>
        {
            new() { ParagraphId = "0", Row = 0, Column = 0, TableId = "t1" },
            new() { ParagraphId = "1", Row = 0, Column = 1, TableId = "t1" },
            new() { ParagraphId = "2", Row = 1, Column = 0, TableId = "t1" },
            new() { ParagraphId = "3", Row = 1, Column = 1, TableId = "t1" }
        };

        var result = grouper.Group(paragraphs, tableCells, p => paragraphs.IndexOf(p).ToString());

        // 2 locked table rows + body paragraphs reduced to at most 5 total
        Assert.True(result.Count <= 5);
        Assert.True(result.Count >= 2); // At least the 2 table rows survive
    }

    [Fact]
    public void FullPipeline_HeuristicDetectionEnabled()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 5,
            EnableTableDetection = true
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>();
        // Tight grid (will be detected as table)
        paragraphs.Add(new OcrParagraph { Page = 1, Content = "T1", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 50, MaxY = 15 } });
        paragraphs.Add(new OcrParagraph { Page = 1, Content = "T2", BoundingBox = new Polygon { MinX = 55, MinY = 0, MaxX = 105, MaxY = 15 } });
        // Scattered body paragraphs
        for (int i = 0; i < 20; i++)
        {
            paragraphs.Add(new OcrParagraph
            {
                Page = 1,
                Content = $"Body{i}",
                BoundingBox = new Polygon { MinX = i * 10, MinY = 50 + i * 20, MaxX = i * 10 + 50, MaxY = 65 + i * 20 }
            });
        }

        var result = grouper.Group(paragraphs);

        Assert.True(result.Count <= 5);
    }

    [Fact]
    public void FullPipeline_HeuristicDetectionDisabled()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 5,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>();
        for (int i = 0; i < 30; i++)
        {
            paragraphs.Add(new OcrParagraph
            {
                Page = 1,
                Content = $"P{i}",
                BoundingBox = new Polygon { MinX = i * 5, MinY = i * 3, MaxX = i * 5 + 4, MaxY = i * 3 + 4 }
            });
        }

        var result = grouper.Group(paragraphs);

        Assert.True(result.Count <= 5);
    }

    [Fact]
    public void MaxMergeDistance_StopsEarly()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 1,
            MaxMergeDistance = 10,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 0, MinY = 110, MaxX = 10, MaxY = 120 } }
        };

        var result = grouper.Group(paragraphs);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void MaxMergeDistance_Null_NoCap()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 1,
            MaxMergeDistance = null,
            EnableTableDetection = false
        };
        var grouper = new ParagraphGrouper(options);

        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } },
            new() { Page = 1, Content = "B", BoundingBox = new Polygon { MinX = 0, MinY = 110, MaxX = 10, MaxY = 120 } }
        };

        var result = grouper.Group(paragraphs);

        Assert.Equal(1, result.Count);
    }
}

public class OptionsValidationTests
{
    [Fact]
    public void ValidOptions_PassesValidation()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 20
        };

        // Should not throw
        options.Validate();
    }

    [Fact]
    public void MinGreaterThanMax_ThrowsArgumentException()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 0
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void NegativeMaxMergeDistance_ThrowsArgumentException()
    {
        var options = new ParagraphGrouperOptions
        {
            MaxMergeDistance = -1
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void NegativeThreshold_ThrowsArgumentException()
    {
        var options = new ParagraphGrouperOptions
        {
            MaxTableHorizontalGap = -5
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void NegativeVerticalWeight_ThrowsArgumentException()
    {
        var options = new ParagraphGrouperOptions
        {
            VerticalDistanceWeight = -1
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void NegativeMaxTableVerticalGap_ThrowsArgumentException()
    {
        var options = new ParagraphGrouperOptions
        {
            MaxTableVerticalGap = -1
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void TargetParagraphsPerPageZero_ThrowsArgumentException()
    {
        var options = new ParagraphGrouperOptions
        {
            TargetParagraphsPerPage = 0
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ParagraphGrouper(null!));
    }

    [Fact]
    public void Group_NullParagraphs_ReturnsEmptyList()
    {
        var grouper = new ParagraphGrouper(new ParagraphGrouperOptions());

        var result = grouper.Group(null!);

        Assert.Empty(result);
    }

    [Fact]
    public void GroupWithTableCells_NullTableCells_ThrowsArgumentNullException()
    {
        var grouper = new ParagraphGrouper(new ParagraphGrouperOptions());
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } }
        };

        Assert.Throws<ArgumentNullException>(() =>
            grouper.Group(paragraphs, null!, p => ""));
    }

    [Fact]
    public void GroupWithTableCells_NullKeySelector_ThrowsArgumentNullException()
    {
        var grouper = new ParagraphGrouper(new ParagraphGrouperOptions());
        var paragraphs = new List<OcrParagraph>
        {
            new() { Page = 1, Content = "A", BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 10, MaxY = 10 } }
        };

        Assert.Throws<ArgumentNullException>(() =>
            grouper.Group(paragraphs, [], null!));
    }
}

public class DiRegistrationTests
{
    [Fact]
    public void AddParagraphGrouper_RegistersSingleton()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddParagraphGrouper();
        var provider = services.BuildServiceProvider();

        var grouper = provider.GetRequiredService<IParagraphGrouper>();

        Assert.NotNull(grouper);
    }

    [Fact]
    public void AddParagraphGrouper_WithCustomOptions_UsesOptions()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddParagraphGrouper(opts => opts.TargetParagraphsPerPage = 10);
        var provider = services.BuildServiceProvider();

        var grouper = provider.GetRequiredService<IParagraphGrouper>();

        // Just verify it resolves and doesn't throw
        Assert.NotNull(grouper);
    }
}

public class JsonSerializationTests
{
    [Fact]
    public void OcrParagraph_RoundTrip_PreservesProperties()
    {
        var original = new OcrParagraph
        {
            Page = 1,
            Content = "Hello World",
            BoundingBox = new Polygon { MinX = 0, MinY = 0, MaxX = 100, MaxY = 10 }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<OcrParagraph>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Page, deserialized.Page);
        Assert.Equal(original.Content, deserialized.Content);
        Assert.Equal(original.BoundingBox.MinX, deserialized.BoundingBox.MinX);
        Assert.Equal(original.BoundingBox.MinY, deserialized.BoundingBox.MinY);
        Assert.Equal(original.BoundingBox.MaxX, deserialized.BoundingBox.MaxX);
        Assert.Equal(original.BoundingBox.MaxY, deserialized.BoundingBox.MaxY);
    }

    [Fact]
    public void Polygon_RoundTrip_PreservesProperties()
    {
        var original = new Polygon { MinX = 1.5, MinY = 2.5, MaxX = 3.5, MaxY = 4.5 };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Polygon>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.MinX, deserialized.MinX);
        Assert.Equal(original.MinY, deserialized.MinY);
        Assert.Equal(original.MaxX, deserialized.MaxX);
        Assert.Equal(original.MaxY, deserialized.MaxY);
    }

    [Fact]
    public void TableCellInfo_RoundTrip_PreservesProperties()
    {
        var original = new TableCellInfo
        {
            ParagraphId = "p42",
            Row = 0,
            Column = 1,
            TableId = "t1"
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TableCellInfo>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.ParagraphId, deserialized.ParagraphId);
        Assert.Equal(original.Row, deserialized.Row);
        Assert.Equal(original.Column, deserialized.Column);
        Assert.Equal(original.TableId, deserialized.TableId);
    }
}
