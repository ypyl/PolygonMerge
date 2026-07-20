using System.Text.Json.Serialization;

namespace PolygonMerge;

/// <summary>
/// Metadata linking an OCR paragraph to a table cell position.
/// Provided by upstream OCR engines (e.g., Azure Document Intelligence) that already
/// know which paragraphs belong to which table cells.
/// </summary>
public class TableCellInfo
{
    /// <summary>
    /// Identifies which OcrParagraph this cell corresponds to.
    /// Matched via a user-provided key selector function.
    /// </summary>
    [JsonPropertyName("paragraphId")]
    public string ParagraphId { get; set; } = string.Empty;

    /// <summary>
    /// Zero-based row index within the table.
    /// </summary>
    [JsonPropertyName("row")]
    public int Row { get; set; }

    /// <summary>
    /// Zero-based column index within the table.
    /// </summary>
    [JsonPropertyName("column")]
    public int Column { get; set; }

    /// <summary>
    /// Optional table identifier to distinguish multiple tables on the same page.
    /// When null or empty, all cells on the same page are treated as belonging to one logical table.
    /// </summary>
    [JsonPropertyName("tableId")]
    public string? TableId { get; set; }
}
