using System.Security.Cryptography;
using System.Text;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace MaintenanceManualsIndexer;

public class PdfDocumentIndexer(
    IPdfTextExtractor textExtractor,
    EmbeddingClient embeddingClient,
    SearchIndexClient indexClient,
    ILogger<PdfDocumentIndexer> logger)
{
    private const int UploadBatchSize = 64;

    private readonly SearchClient _childClient = indexClient.GetSearchClient(SearchIndexCreator.ChildIndexName);
    private readonly SearchClient _parentClient = indexClient.GetSearchClient(SearchIndexCreator.ParentIndexName);

    /// <summary>
    /// Extracts, chunks, embeds, and indexes a PDF, replacing obsolete chunks for the same file path.
    /// </summary>
    /// <param name="filePath">The path to a local PDF file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of child chunks uploaded.</returns>
    public async Task<int> IndexPdfAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var fullPath = Path.GetFullPath(filePath);
        if (!string.Equals(Path.GetExtension(fullPath), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The input file must have a .pdf extension.", nameof(filePath));

        BinaryData pdf;
        await using (var stream = File.OpenRead(fullPath))
        {
            var header = new byte[5];
            if (stream.Length < header.Length)
                throw new InvalidDataException("The input file is too short to be a PDF.");

            await stream.ReadExactlyAsync(header, cancellationToken);
            if (!header.AsSpan().SequenceEqual("%PDF-"u8))
                throw new InvalidDataException("The input file does not have a PDF header.");

            stream.Position = 0;
            pdf = await BinaryData.FromStreamAsync(stream, cancellationToken);
        }

        logger.LogInformation("Extracting PDF text with {Provider}.", textExtractor.Name);
        var extractedText = await textExtractor.ExtractTextAsync(pdf, cancellationToken);
        if (extractedText.ChunkCount == 0 || extractedText.Chunks.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException($"No complete, non-empty chunks were extracted from '{fullPath}'.");

        var normalizedPath = OperatingSystem.IsWindows() ? fullPath.ToUpperInvariant() : fullPath;
        var parentId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath))).ToLowerInvariant();
        var existingIds = await GetExistingChunkIdsAsync(parentId, cancellationToken);
        var options = new IndexDocumentsOptions { ThrowOnAnyError = true };
        var embeddingOptions = new EmbeddingGenerationOptions { Dimensions = SearchIndexCreator.VectorDimensions };
        HashSet<string> currentIds = [];

        await _parentClient.UploadDocumentsAsync(
            new[]
            {
                new SearchDocument
                {
                    ["parent_id"] = parentId,
                    ["title"] = Path.GetFileNameWithoutExtension(fullPath),
                    ["abstract"] = extractedText.Summary
                }
            },
            options,
            cancellationToken);

        List<SearchDocument> batch = [];
        for (var i = 0; i < extractedText.ChunkCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var embedding = await embeddingClient.GenerateEmbeddingAsync(extractedText.Chunks[i], embeddingOptions, cancellationToken);
            var vector = embedding.Value.ToFloats().ToArray();
            if (vector.Length != SearchIndexCreator.VectorDimensions)
                throw new InvalidOperationException(
                    $"The embedding has {vector.Length} dimensions; the child index requires {SearchIndexCreator.VectorDimensions}.");

            var chunkId = $"{parentId}_{i:D6}";
            currentIds.Add(chunkId);
            batch.Add(new SearchDocument
            {
                ["chunk_id"] = chunkId,
                ["parent_id"] = parentId,
                ["chunk"] = extractedText.Chunks[i],
                ["chunk_vector"] = vector
            });

            if (batch.Count == UploadBatchSize || i == extractedText.ChunkCount - 1)
            {
                await _childClient.UploadDocumentsAsync(batch, options, cancellationToken);
                batch.Clear();
                logger.LogInformation("Uploaded {Uploaded} of {Total} chunks.", i + 1, extractedText.ChunkCount);
            }
        }

        foreach (var obsoleteIds in existingIds.Except(currentIds).Chunk(UploadBatchSize))
        {
            await _childClient.DeleteDocumentsAsync("chunk_id", obsoleteIds, options, cancellationToken);
        }

        logger.LogInformation("Indexed parent {ParentId} with {Count} child chunks.", parentId, extractedText.ChunkCount);
        return extractedText.ChunkCount;
    }

    private async Task<HashSet<string>> GetExistingChunkIdsAsync(string parentId, CancellationToken cancellationToken)
    {
        var options = new SearchOptions
        {
            Filter = SearchFilter.Create($"parent_id eq {parentId}")
        };
        options.Select.Add("chunk_id");

        var response = await _childClient.SearchAsync<SearchDocument>("*", options, cancellationToken);
        HashSet<string> ids = [];
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            if (result.Document.TryGetValue("chunk_id", out var value) && value is string id)
                ids.Add(id);
            else
                throw new InvalidOperationException("A child search result is missing its chunk_id.");
        }

        return ids;
    }
}
