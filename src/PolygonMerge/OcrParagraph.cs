using System.Text.Json.Serialization;

namespace PolygonMerge;

/// <summary>
/// Represents a single OCR-detected paragraph with page number, text content, and bounding polygon.
/// </summary>
public class OcrParagraph
{
    /// <summary>The page number this paragraph belongs to.</summary>
    [JsonPropertyName("page")]
    public int Page { get; set; }

    /// <summary>The text content of the paragraph.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>The bounding polygon (axis-aligned bounding box) of the paragraph.</summary>
    [JsonPropertyName("boundingBox")]
    public Polygon BoundingBox { get; set; } = new();
}
