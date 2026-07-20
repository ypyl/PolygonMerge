using System.Text.Json.Serialization;

namespace PolygonMerge;

/// <summary>
/// Represents an axis-aligned bounding box defined by its min/max coordinates.
/// </summary>
public class Polygon
{
    /// <summary>Left edge X coordinate.</summary>
    [JsonPropertyName("minX")]
    public double MinX { get; set; }

    /// <summary>Top edge Y coordinate.</summary>
    [JsonPropertyName("minY")]
    public double MinY { get; set; }

    /// <summary>Right edge X coordinate.</summary>
    [JsonPropertyName("maxX")]
    public double MaxX { get; set; }

    /// <summary>Bottom edge Y coordinate.</summary>
    [JsonPropertyName("maxY")]
    public double MaxY { get; set; }

    /// <summary>
    /// Merges two polygons into a single bounding box that encompasses both.
    /// </summary>
    public static Polygon Merge(Polygon a, Polygon b)
    {
        return new Polygon
        {
            MinX = Math.Min(a.MinX, b.MinX),
            MinY = Math.Min(a.MinY, b.MinY),
            MaxX = Math.Max(a.MaxX, b.MaxX),
            MaxY = Math.Max(a.MaxY, b.MaxY)
        };
    }

    /// <summary>
    /// Merges multiple polygons into a single bounding box that encompasses all.
    /// </summary>
    public static Polygon Merge(IEnumerable<Polygon> polygons)
    {
        return polygons.Aggregate(Merge);
    }

    /// <summary>
    /// Computes the weighted AABB distance between this polygon and another.
    /// Edge-to-edge gaps: zero on an axis if projections overlap.
    /// Vertical weight multiplier gives reading-order-aware proximity.
    /// </summary>
    public double DistanceTo(Polygon other, double verticalWeight = 2.0)
    {
        double xDist = Math.Max(0, Math.Max(MinX - other.MaxX, other.MinX - MaxX));
        double yDist = Math.Max(0, Math.Max(MinY - other.MaxY, other.MinY - MaxY));

        return Math.Sqrt(xDist * xDist + (yDist * verticalWeight) * (yDist * verticalWeight));
    }
}
