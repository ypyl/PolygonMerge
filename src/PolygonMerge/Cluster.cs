namespace PolygonMerge;

/// <summary>
/// Internal cluster holding one or more OcrParagraph instances.
/// Locked clusters (from table detection) must not be merged with other locked clusters during HAC.
/// </summary>
internal class Cluster
{
    public List<OcrParagraph> Paragraphs { get; }
    public bool IsLocked { get; set; }

    private Polygon? _cachedPolygon;

    public Cluster(OcrParagraph paragraph, bool isLocked = false)
    {
        Paragraphs = [paragraph];
        IsLocked = isLocked;
    }

    public Cluster(List<OcrParagraph> paragraphs, bool isLocked = false)
    {
        Paragraphs = paragraphs;
        IsLocked = isLocked;
    }

    /// <summary>
    /// Returns the merged bounding polygon of all paragraphs in this cluster.
    /// Result is cached until the cluster is mutated.
    /// </summary>
    public Polygon GetTotalPolygon()
    {
        if (_cachedPolygon == null)
        {
            _cachedPolygon = Polygon.Merge(Paragraphs.Select(p => p.BoundingBox));
        }
        return _cachedPolygon;
    }

    /// <summary>
    /// Merges another cluster into this one. Content is appended in reading order.
    /// The resulting cluster inherits the locked state of this one.
    /// </summary>
    public void Merge(Cluster other)
    {
        Paragraphs.AddRange(other.Paragraphs);
        _cachedPolygon = null;

        // If either was locked, the result stays locked
        // (but HAC should never merge two locked clusters together)
    }

    /// <summary>
    /// Returns the content of all paragraphs joined in reading order (top-to-bottom, left-to-right).
    /// </summary>
    public string GetOrderedContent()
    {
        var ordered = Paragraphs
            .OrderBy(p => p.BoundingBox.MinY)
            .ThenBy(p => p.BoundingBox.MinX)
            .Select(p => p.Content);

        return string.Join(" ", ordered);
    }
}
