namespace MaintenanceManualsIndexer;

/// <summary>
/// Extracts indexable text from PDF bytes.
/// </summary>
public interface IPdfTextExtractor
{
    /// <summary>Gets the name of the extraction provider.</summary>
    string Name { get; }

    /// <summary>Extracts text from a PDF.</summary>
    /// <param name="pdf">The PDF bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The extracted text.</returns>
    Task<ExtractedText> ExtractTextAsync(BinaryData pdf, CancellationToken cancellationToken = default);
}

/// <summary>Contains the extracted document text, chunks, and optional generated summary.</summary>
public class ExtractedText
{
    /// <summary>The extracted text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the generated document abstract, when supported by the provider.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets all indexable text chunks.</summary>
    public IReadOnlyList<string> Chunks { get; set; } = new List<string>();

    /// <summary>Gets the number of chunks.</summary>
    public int ChunkCount => Chunks.Count;
}