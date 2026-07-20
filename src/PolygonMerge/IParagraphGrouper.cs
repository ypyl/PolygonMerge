namespace PolygonMerge;

/// <summary>
/// Service interface for merging fragmented OCR paragraphs into larger, coherent segments.
/// </summary>
public interface IParagraphGrouper
{
    /// <summary>
    /// Groups paragraphs using spatial clustering only (heuristic table detection if enabled in options).
    /// </summary>
    /// <param name="paragraphs">The input OCR paragraphs.</param>
    /// <returns>Merged paragraphs with count reduced to the configured target range per page.</returns>
    List<OcrParagraph> Group(List<OcrParagraph> paragraphs);

    /// <summary>
    /// Groups paragraphs using explicit table structure metadata from the upstream OCR engine.
    /// Table cells in the same row are merged into locked clusters before spatial clustering.
    /// The heuristic table detection option is ignored when this overload is used.
    /// </summary>
    /// <param name="paragraphs">The input OCR paragraphs.</param>
    /// <param name="tableCells">Table cell metadata linking paragraphs to (row, column, table) positions.</param>
    /// <param name="paragraphKeySelector">Function to extract a key from an OcrParagraph for matching with TableCellInfo.ParagraphId.</param>
    /// <returns>Merged paragraphs with count reduced to the configured target range per page.</returns>
    List<OcrParagraph> Group(
        List<OcrParagraph> paragraphs,
        List<TableCellInfo> tableCells,
        Func<OcrParagraph, string> paragraphKeySelector);
}
