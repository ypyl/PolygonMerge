using Microsoft.Extensions.DependencyInjection;

namespace PolygonMerge;

/// <summary>
/// Main service that merges fragmented OCR paragraphs into larger, coherent segments.
/// Uses a two-pass algorithm: table structure pre-pass (explicit or heuristic) followed by
/// hierarchical agglomerative clustering.
/// </summary>
public class ParagraphGrouper : IParagraphGrouper
{
    private readonly ParagraphGrouperOptions _options;

    /// <summary>
    /// Creates a new ParagraphGrouper with the specified options.
    /// </summary>
    /// <param name="options">Configuration options. Must not be null.</param>
    public ParagraphGrouper(ParagraphGrouperOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
    }

    /// <inheritdoc />
    public List<OcrParagraph> Group(List<OcrParagraph> paragraphs)
    {
        if (paragraphs == null || paragraphs.Count == 0)
            return [];

        // Group by page
        var pages = paragraphs.GroupBy(p => p.Page);

        var result = new List<OcrParagraph>();

        foreach (var pageGroup in pages)
        {
            var clusters = pageGroup.Select(p => new Cluster(p)).ToList();

            // Heuristic table detection (if enabled)
            if (_options.EnableTableDetection)
            {
                clusters = CollapseTableZonesHeuristic(clusters);
            }

            // HAC reduction
            clusters = ReduceClusters(clusters);

            // Flatten
            result.AddRange(FlattenClusters(clusters));
        }

        return result;
    }

    /// <inheritdoc />
    public List<OcrParagraph> Group(
        List<OcrParagraph> paragraphs,
        List<TableCellInfo> tableCells,
        Func<OcrParagraph, string> paragraphKeySelector)
    {
        if (paragraphs == null || paragraphs.Count == 0)
            return [];

        if (tableCells == null)
            throw new ArgumentNullException(nameof(tableCells));

        if (paragraphKeySelector == null)
            throw new ArgumentNullException(nameof(paragraphKeySelector));

        // Group by page
        var pages = paragraphs.GroupBy(p => p.Page);

        var result = new List<OcrParagraph>();

        foreach (var pageGroup in pages)
        {
            var clusters = pageGroup.Select(p => new Cluster(p)).ToList();

            // Explicit table structure pre-pass
            clusters = CollapseTableRows(clusters, tableCells, paragraphKeySelector);

            // HAC reduction
            clusters = ReduceClusters(clusters);

            // Flatten
            result.AddRange(FlattenClusters(clusters));
        }

        return result;
    }

    // ---- Placeholder methods (implemented in later tasks) ----

    /// <summary>
    /// Determines if two polygons are table neighbors (share row or column alignment with small gap).
    /// </summary>
    private bool IsTableNeighbor(Polygon a, Polygon b)
    {
        double xDist = Math.Max(0, Math.Max(a.MinX - b.MaxX, b.MinX - a.MaxX));
        double yDist = Math.Max(0, Math.Max(a.MinY - b.MaxY, b.MinY - a.MaxY));

        bool sharesRow = a.MinY < b.MaxY && b.MinY < a.MaxY;  // Y-ranges overlap
        bool sharesCol = a.MinX < b.MaxX && b.MinX < a.MaxX;  // X-ranges overlap

        // Same row: Y-ranges overlap, small horizontal gap
        if (sharesRow && xDist <= _options.MaxTableHorizontalGap)
            return true;

        // Same column: X-ranges overlap, small vertical gap
        if (sharesCol && yDist <= _options.MaxTableVerticalGap)
            return true;

        return false;
    }

    private List<Cluster> CollapseTableZonesHeuristic(List<Cluster> clusters)
    {
        bool mergedAny;

        do
        {
            mergedAny = false;

            for (int i = 0; i < clusters.Count; i++)
            {
                var polyA = clusters[i].GetTotalPolygon();

                for (int j = i + 1; j < clusters.Count; j++)
                {
                    var polyB = clusters[j].GetTotalPolygon();

                    if (IsTableNeighbor(polyA, polyB))
                    {
                        clusters[i].Merge(clusters[j]);
                        clusters.RemoveAt(j);
                        mergedAny = true;
                        break;
                    }
                }

                if (mergedAny)
                    break;
            }
        } while (mergedAny);

        // Mark all collapsed zones as locked
        // (any cluster that grew beyond its original single paragraph is a table zone)
        foreach (var cluster in clusters)
        {
            if (cluster.Paragraphs.Count > 1)
                cluster.IsLocked = true;
        }

        return clusters;
    }

    private static List<Cluster> CollapseTableRows(
        List<Cluster> clusters,
        List<TableCellInfo> tableCells,
        Func<OcrParagraph, string> paragraphKeySelector)
    {
        // Build a lookup from paragraph key → cluster index
        var keyToIndex = new Dictionary<string, int>();
        for (int i = 0; i < clusters.Count; i++)
        {
            var key = paragraphKeySelector(clusters[i].Paragraphs[0]);
            if (!string.IsNullOrEmpty(key))
                keyToIndex[key] = i;
        }

        // Map each matched cluster index to its (page, tableId, row, column)
        var cellInfo = new Dictionary<int, (int Page, string TableId, int Row, int Column)>();
        foreach (var cell in tableCells)
        {
            if (!keyToIndex.TryGetValue(cell.ParagraphId, out int idx))
                continue;
            int page = clusters[idx].Paragraphs[0].Page;
            string tableId = cell.TableId ?? string.Empty;
            cellInfo[idx] = (page, tableId, cell.Row, cell.Column);
        }

        // Group cluster indices by (Page, TableId, Row), ordered by Column
        var rowGroups = cellInfo
            .GroupBy(kv => (kv.Value.Page, kv.Value.TableId, kv.Value.Row))
            .Select(g => g.OrderBy(kv => kv.Value.Column).Select(kv => kv.Key).ToList())
            .ToList();

        var consumed = new HashSet<int>();
        var result = new List<Cluster>();

        // Create locked clusters for each row
        foreach (var indices in rowGroups)
        {
            foreach (var idx in indices)
                consumed.Add(idx);

            var mergedParagraphs = indices.SelectMany(idx => clusters[idx].Paragraphs).ToList();
            result.Add(new Cluster(mergedParagraphs, isLocked: true));
        }

        // Add unmatched (free) clusters
        for (int i = 0; i < clusters.Count; i++)
        {
            if (!consumed.Contains(i))
                result.Add(clusters[i]);
        }

        return result;
    }

    private List<Cluster> ReduceClusters(List<Cluster> clusters)
    {
        int targetCount = _options.MaxParagraphsPerPage;

        if (clusters.Count <= targetCount)
            return clusters;

        while (clusters.Count > targetCount)
        {
            double minDistance = double.MaxValue;
            int mergeA = -1, mergeB = -1;

            for (int i = 0; i < clusters.Count; i++)
            {
                var polyA = clusters[i].GetTotalPolygon();

                for (int j = i + 1; j < clusters.Count; j++)
                {
                    // Never merge two locked clusters together
                    if (clusters[i].IsLocked && clusters[j].IsLocked)
                        continue;

                    var polyB = clusters[j].GetTotalPolygon();
                    double dist = polyA.DistanceTo(polyB, _options.VerticalDistanceWeight);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        mergeA = i;
                        mergeB = j;
                    }
                }
            }

            if (mergeA == -1)
                break; // All remaining pairs are locked-locked; stop

            clusters[mergeA].Merge(clusters[mergeB]);
            clusters.RemoveAt(mergeB);
        }

        return clusters;
    }

    private static List<OcrParagraph> FlattenClusters(List<Cluster> clusters)
    {
        return clusters.Select(c => new OcrParagraph
        {
            Page = c.Paragraphs[0].Page,
            Content = c.GetOrderedContent(),
            BoundingBox = c.GetTotalPolygon()
        }).ToList();
    }
}

/// <summary>
/// DI registration extensions for PolygonMerge.
/// Excluded from code coverage — this is glue code tested via integration-level DI tests.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class ParagraphGrouperServiceExtensions
{
    /// <summary>
    /// Registers <see cref="IParagraphGrouper"/> as a singleton with default or customized options.
    /// </summary>
    public static IServiceCollection AddParagraphGrouper(
        this IServiceCollection services,
        Action<ParagraphGrouperOptions>? configure = null)
    {
        var options = new ParagraphGrouperOptions();
        configure?.Invoke(options);

        services.AddSingleton<IParagraphGrouper>(_ => new ParagraphGrouper(options));
        return services;
    }
}
