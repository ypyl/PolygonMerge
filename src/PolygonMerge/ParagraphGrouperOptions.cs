namespace PolygonMerge;

/// <summary>
/// Configuration options for the paragraph grouping pipeline.
/// All properties have sensible defaults so <c>new ParagraphGrouperOptions()</c> works out of the box.
/// </summary>
public class ParagraphGrouperOptions
{
    /// <summary>
    /// Target maximum number of paragraphs per page. The HAC loop stops when the cluster count
    /// reaches or falls below this value. Must be ≥ 1. Default: 20.
    /// </summary>
    public int TargetParagraphsPerPage { get; set; } = 20;

    /// <summary>
    /// Optional distance safety valve. When set, the HAC loop stops early if the closest pair of
    /// clusters exceeds this distance, even if the target count has not been reached.
    /// When null, no distance cap is applied. Must be ≥ 0 if set. Default: null.
    /// </summary>
    public double? MaxMergeDistance { get; set; } = null;

    /// <summary>
    /// Weight multiplier for Y-axis distance in AABB calculations.
    /// Higher values prioritize vertical proximity (reading order) over horizontal.
    /// Must be ≥ 0. Default: 2.0.
    /// </summary>
    public double VerticalDistanceWeight { get; set; } = 2.0;

    /// <summary>
    /// Maximum horizontal gap (in points) between table cells in the same row.
    /// Used only by the heuristic table detection fallback. Default: 25.0.
    /// </summary>
    public double MaxTableHorizontalGap { get; set; } = 25.0;

    /// <summary>
    /// Maximum vertical gap (in points) between table cells in the same column.
    /// Used only by the heuristic table detection fallback. Default: 15.0.
    /// </summary>
    public double MaxTableVerticalGap { get; set; } = 15.0;

    /// <summary>
    /// Whether to run the heuristic table detection pre-pass when no explicit table structure is provided.
    /// Ignored when explicit TableCellInfo data is supplied. Default: true.
    /// </summary>
    public bool EnableTableDetection { get; set; } = true;

    /// <summary>
    /// Validates the options and throws if any values are invalid.
    /// </summary>
    public void Validate()
    {
        if (TargetParagraphsPerPage < 1)
            throw new ArgumentException($"{nameof(TargetParagraphsPerPage)} must be at least 1.");

        if (MaxMergeDistance.HasValue && MaxMergeDistance.Value < 0)
            throw new ArgumentException($"{nameof(MaxMergeDistance)} must be non-negative if set.");

        if (VerticalDistanceWeight < 0)
            throw new ArgumentException($"{nameof(VerticalDistanceWeight)} must be non-negative.");

        if (MaxTableHorizontalGap < 0)
            throw new ArgumentException($"{nameof(MaxTableHorizontalGap)} must be non-negative.");

        if (MaxTableVerticalGap < 0)
            throw new ArgumentException($"{nameof(MaxTableVerticalGap)} must be non-negative.");
    }
}
