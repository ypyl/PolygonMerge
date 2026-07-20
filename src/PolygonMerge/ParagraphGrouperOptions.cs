namespace PolygonMerge;

/// <summary>
/// Configuration options for the paragraph grouping pipeline.
/// All properties have sensible defaults so <c>new ParagraphGrouperOptions()</c> works out of the box.
/// </summary>
public class ParagraphGrouperOptions
{
    /// <summary>
    /// Minimum target number of paragraphs per page. Must be ≥ 1 and ≤ MaxParagraphsPerPage.
    /// Default: 6.
    /// </summary>
    public int MinParagraphsPerPage { get; set; } = 6;

    /// <summary>
    /// Maximum target number of paragraphs per page. The HAC loop stops when cluster count reaches this value.
    /// Default: 20.
    /// </summary>
    public int MaxParagraphsPerPage { get; set; } = 20;

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
        if (MinParagraphsPerPage < 1)
            throw new ArgumentException($"{nameof(MinParagraphsPerPage)} must be at least 1.");

        if (MinParagraphsPerPage > MaxParagraphsPerPage)
            throw new ArgumentException(
                $"{nameof(MinParagraphsPerPage)} ({MinParagraphsPerPage}) must be ≤ {nameof(MaxParagraphsPerPage)} ({MaxParagraphsPerPage}).");

        if (VerticalDistanceWeight < 0)
            throw new ArgumentException($"{nameof(VerticalDistanceWeight)} must be non-negative.");

        if (MaxTableHorizontalGap < 0)
            throw new ArgumentException($"{nameof(MaxTableHorizontalGap)} must be non-negative.");

        if (MaxTableVerticalGap < 0)
            throw new ArgumentException($"{nameof(MaxTableVerticalGap)} must be non-negative.");
    }
}
