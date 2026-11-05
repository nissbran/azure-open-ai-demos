using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.KnowledgeBases;
using Azure.Search.Documents.KnowledgeBases.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SwapiIndexer;

public class SearchTestReader
{
    private readonly SearchClient _searchClient;
    private readonly KnowledgeBaseRetrievalClient _knowledgeBaseRetrievalClient;

    private const string IndexName = "swapi-vehicle-index";
    private const string KnowledgeBaseId = "vehicle-parts-and-information";

    public SearchTestReader(IConfiguration configuration)
    {
        var endpoint = new Uri(configuration["AzureAISearch:Endpoint"]);
        var credential = new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]);
        _knowledgeBaseRetrievalClient = new KnowledgeBaseRetrievalClient(endpoint, KnowledgeBaseId, credential);
        _searchClient = new SearchClient(endpoint, IndexName, credential);
    }

    public async Task ReadTestAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _knowledgeBaseRetrievalClient.RetrieveAsync(new KnowledgeBaseRetrievalRequest
        {
            Intents =
            {
                new KnowledgeRetrievalSemanticIntent("Get me the bill of materials for the hauler 500?")
            }
        });
        stopwatch.Stop();
        Log.Information("Knowledge base retrieval took {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

        foreach (var knowledgeBaseMessage in response.Value.Response)
        {
            foreach (var knowledgeBaseMessageContent1 in knowledgeBaseMessage.Content)
            {
                var knowledgeBaseMessageContent = (KnowledgeBaseMessageTextContent)knowledgeBaseMessageContent1;
                Log.Information("Message: {Message}", knowledgeBaseMessageContent.Text);
            }
        }
        
        stopwatch = Stopwatch.StartNew();
        response = await _knowledgeBaseRetrievalClient.RetrieveAsync(new KnowledgeBaseRetrievalRequest
        {
            Intents =
            {
                new KnowledgeRetrievalSemanticIntent("Get me the bill of materials for the hauler 500?")
            }
        });
        stopwatch.Stop();
        Log.Information("Knowledge base retrieval took {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        
        foreach (var knowledgeBaseMessage in response.Value.Response)
        {
            foreach (var knowledgeBaseMessageContent1 in knowledgeBaseMessage.Content)
            {
                var knowledgeBaseMessageContent = (KnowledgeBaseMessageTextContent)knowledgeBaseMessageContent1;
                Log.Information("Message: {Message}", knowledgeBaseMessageContent.Text);
            }
        }
        
    }
}