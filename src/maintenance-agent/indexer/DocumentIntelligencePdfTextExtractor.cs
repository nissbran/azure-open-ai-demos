using Azure;
using Azure.AI.DocumentIntelligence;

namespace MaintenanceManualsIndexer;

/// <summary>
/// Extracts PDF text using Document Intelligence's layout model.
/// </summary>
/// <param name="client">The Document Intelligence client.</param>
public class DocumentIntelligencePdfTextExtractor(DocumentIntelligenceClient client) : IPdfTextExtractor
{
    /// <inheritdoc/>
    public string Name => "Document Intelligence";

    /// <inheritdoc/>
    public async Task<ExtractedText> ExtractTextAsync(BinaryData pdf, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        var analysis = await client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            new AnalyzeDocumentOptions("prebuilt-layout", pdf),
            cancellationToken);

        var text = analysis.Value.Content;
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException($"{Name} did not extract any non-empty text from the PDF.");

        var chunks = DocumentIntelligenceChunker.Chunk(text);

        return new ExtractedText
        {
            Text = text,
            Chunks = chunks
        };
    }
}
