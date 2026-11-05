using Azure;
using Azure.AI.ContentUnderstanding;

namespace MaintenanceManualsIndexer;

/// <summary>
/// Extracts PDF Markdown using a Content Understanding analyzer.
/// </summary>
public class ContentUnderstandingPdfTextExtractor : IPdfTextExtractor
{
    private readonly string _analyzerId;
    private readonly ContentUnderstandingClient _client;

    /// <summary>Initializes a Content Understanding text extractor.</summary>
    /// <param name="client">The Content Understanding client.</param>
    /// <param name="analyzerId">The analyzer to use for PDF extraction.</param>
    public ContentUnderstandingPdfTextExtractor(ContentUnderstandingClient client, string analyzerId)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(analyzerId);
        _client = client;
        _analyzerId = analyzerId;
    }

    /// <inheritdoc/>
    public string Name => "Content Understanding";

    /// <inheritdoc/>
    public async Task<ExtractedText> ExtractTextAsync(BinaryData pdf, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        var analysis = await _client.AnalyzeBinaryAsync(
            WaitUntil.Completed,
            _analyzerId,
            pdf,
            contentType: "application/pdf",
            cancellationToken: cancellationToken);

        var result = analysis.Value;
        if (result.Contents!.First() is not DocumentContent documentContent)
        {
            throw new InvalidOperationException($"{Name} did not return any document content.");
        }

        var chunkMarkdowns = (documentContent.Chunks ?? Enumerable.Empty<DocumentChunk>())
            .Select(chunk => string.Join(
                Environment.NewLine,
                chunk.Spans.Select(span => documentContent.Markdown.Substring(span.Offset, span.Length))))
            .ToList();
        
        var summary = (documentContent.Fields?.FirstOrDefault(f => f.Key == "abstract_summary").Value as ContentStringField)?.Value;

        return new ExtractedText
        {
            Text = documentContent.Markdown,
            Summary = summary,
            Chunks = chunkMarkdowns
        };
    }
}
