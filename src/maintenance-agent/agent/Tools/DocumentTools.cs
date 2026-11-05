using System.ComponentModel;
using System.Text.Json.Serialization;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.AI;

namespace MaintenanceAgent.Tools;

/// <summary>Searches maintenance manual content and retrieves stored document summaries.</summary>
public sealed class DocumentTools
{
    private readonly SearchClient _chunkClient;
    private readonly SearchClient _documentClient;

    /// <summary>Creates tools for the maintenance manual indexes.</summary>
    /// <param name="configuration">Search endpoint and optional API-key configuration.</param>
    public DocumentTools(IConfiguration configuration)
    {
        var endpoint = configuration["AzureAISearch:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new InvalidOperationException("AzureAISearch:Endpoint is missing.");

        var uri = new Uri(endpoint);
        var apiKey = ""; //configuration["AzureAISearch:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            var credential = new AzureCliCredential();
            _chunkClient = new SearchClient(uri, "maintenance-manual-chunks", credential);
            _documentClient = new SearchClient(uri, "maintenance-manuals", credential);
        }
        else
        {
            var credential = new AzureKeyCredential(apiKey);
            _chunkClient = new SearchClient(uri, "maintenance-manual-chunks", credential);
            _documentClient = new SearchClient(uri, "maintenance-manuals", credential);
        }
    }

    internal DocumentTools(SearchClient chunkClient, SearchClient documentClient)
    {
        _chunkClient = chunkClient;
        _documentClient = documentClient;
    }

    /// <summary>Finds relevant manual passages using hybrid vector and semantic search.</summary>
    /// <param name="query">A natural-language question about maintenance or device information.</param>
    /// <param name="documentId">Optional document ID returned by either tool to restrict the search.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Relevant passages with source IDs, document titles, and search scores.</returns>
    [Description("Search maintenance manual passages using vector search. Use this for specific device information, troubleshooting, and repair procedures. Results include source document IDs and titles.")]
    public async Task<List<DocumentInfo>> SearchDocumentInfo(
        [Description("Natural-language question, such as 'How do I replace the Surface Pro 9 battery?'")] string query,
        [Description("Optional source document ID to search only that manual.")] string? documentId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (documentId is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        var options = new SearchOptions
        {
            Filter = documentId is null ? null : SearchFilter.Create($"parent_id eq {documentId}"),
            Size = 10,
            QueryType = SearchQueryType.Semantic,
            SemanticSearch = new SemanticSearchOptions { SemanticConfigurationName = "default" },
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizableTextQuery(query)
                    {
                        KNearestNeighborsCount = 50,
                        Fields = { "chunk_vector" }
                    }
                }
            }
        };
        //options.Select.Add("chunk_id");
        options.Select.Add("parent_id");
        options.Select.Add("chunk");

        var response = await _chunkClient.SearchAsync<ManualChunk>(query, options, cancellationToken);
        Dictionary<string, string> titles = new(StringComparer.Ordinal);
        List<DocumentInfo> documents = [];
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            var chunk = result.Document;
            if (string.IsNullOrWhiteSpace(chunk.DocumentId) ||
                string.IsNullOrWhiteSpace(chunk.Content))
                throw new InvalidOperationException("A manual search result is missing its source IDs or content.");

            if (!titles.TryGetValue(chunk.DocumentId, out var title))
            {
                var parent = await _documentClient.GetDocumentAsync<ManualDocument>(
                    chunk.DocumentId, cancellationToken: cancellationToken);
                title = parent.Value.Title;
                if (string.IsNullOrWhiteSpace(title))
                    throw new InvalidOperationException($"Manual '{chunk.DocumentId}' has no title.");

                titles.Add(chunk.DocumentId, title);
            }

            documents.Add(new DocumentInfo(chunk.DocumentId, title, chunk.Content, result.Score));
        }

        return documents;
    }

    /// <summary>Retrieves stored summaries for all manuals or a selected document or topic.</summary>
    /// <param name="query">Optional words to search document titles and abstracts.</param>
    /// <param name="documentId">Optional exact document ID returned by a search result.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Document IDs, titles, and stored abstracts for every matching manual.</returns>
    [Description("Get stored summaries of maintenance manuals. Omit query and documentId to list all document summaries, or supply a document ID from SearchDocumentInfo to summarize that manual.")]
    public async Task<List<DocumentSummary>> GetDocumentSummaries(
        [Description("Optional topic or device name to match against document titles and summaries.")] string? query = null,
        [Description("Optional exact source document ID.")] string? documentId = null,
        CancellationToken cancellationToken = default)
    {
        if (query is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (documentId is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        var options = new SearchOptions
        {
            Filter = documentId is null ? null : SearchFilter.Create($"parent_id eq {documentId}"),
            OrderBy = { "title asc" },
            SearchFields = { "title", "abstract" },
            Select = { "parent_id", "title", "abstract" }
        };
        var response = await _documentClient.SearchAsync<ManualDocument>(query ?? "*", options, cancellationToken);
        List<DocumentSummary> summaries = [];
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            var document = result.Document;
            if (string.IsNullOrWhiteSpace(document.DocumentId) || string.IsNullOrWhiteSpace(document.Title))
                throw new InvalidOperationException("An indexed manual is missing its document ID or title.");
            if (string.IsNullOrWhiteSpace(document.Summary))
                throw new InvalidOperationException(
                    $"Manual '{document.Title}' has no stored summary. Reindex it with the Content Understanding manual analyzer.");

            summaries.Add(new DocumentSummary(document.DocumentId, document.Title, document.Summary));
        }

        return summaries;
    }

    /// <summary>Exposes both document tools to the agent.</summary>
    /// <returns>The vector-search and document-summary AI functions.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(SearchDocumentInfo);
        yield return AIFunctionFactory.Create(GetDocumentSummaries);
    }

    private sealed record ManualChunk(
        [property: JsonPropertyName("parent_id")] string DocumentId,
        [property: JsonPropertyName("chunk")] string Content);

    private sealed record ManualDocument(
        [property: JsonPropertyName("parent_id")] string DocumentId,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("abstract")] string? Summary);
}

/// <summary>A retrieved manual passage with its source information.</summary>
/// <param name="DocumentId">The parent document ID.</param>
/// <param name="Title">The manual title.</param>
/// <param name="Content">The retrieved passage text.</param>
/// <param name="Score">The search relevance score.</param>
public sealed record DocumentInfo(string DocumentId, string Title, string Content, double? Score);

/// <summary>A stored document summary with its source information.</summary>
/// <param name="DocumentId">The parent document ID.</param>
/// <param name="Title">The manual title.</param>
/// <param name="Summary">The indexed document abstract.</param>
public sealed record DocumentSummary(string DocumentId, string Title, string Summary);
