using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;
using Serilog;

namespace Demo3;

public class SwapiAzureAiSearchFunction : IGptFunction
{
    private readonly SearchClient _searchClient;
    private readonly string _model;
    private readonly OpenAIClient _client;
    private readonly EmbeddingClient _embeddingClient;
    private const string FunctionName = "call_vehicle_search";

    public SwapiAzureAiSearchFunction(IConfiguration configuration)
    {
        _searchClient = new SearchClient(new Uri(configuration["AzureAISearch:Endpoint"]), "swapi-vehicle-index", new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
        _model = configuration["AzureOpenAI:EmbeddingModel"];
        _client = new AzureOpenAIClient(new Uri(configuration["AzureOpenAI:Endpoint"]), new AzureKeyCredential(configuration["AzureOpenAI:ApiKey"]));
        _embeddingClient = _client.GetEmbeddingClient(_model);
    }

    public FunctionToolDefinition GetFunctionDefinition()
    {
        return new FunctionToolDefinition(FunctionName, "Searches for a vehicle in Star Wars.", BinaryData.FromObjectAsJson(
            new
            {
                Type = "object",
                Properties = new
                {
                    search_query = new
                    {
                        Type = "string",
                        Description = "The search query",
                    }
                },
                Required = new[] { "search_query" },
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    public string GetFunctionName() => FunctionName;

    public async Task<string> GetVehicles(SwapiAzureAiSearchFunctionParameters parameters)
    {
        Log.Verbose("Searching for vehicles with query {SearchQuery}", parameters.SearchQuery);
        
        var embeddingsResult = await _embeddingClient.GenerateEmbeddingAsync(parameters.SearchQuery);

        Log.Verbose("Embeddings result: {EmbeddingsResult}", embeddingsResult.Value.ToFloats());
        
        var searchResponse = await _searchClient.SearchAsync<VehicleSearchResult>(parameters.SearchQuery, new SearchOptions()
        {
            QueryType = SearchQueryType.Semantic,
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(embeddingsResult.Value.ToFloats())
                    {
                        KNearestNeighborsCount = 3,
                        Fields = { "summary_vector" }
                    },
                }
            },
            SemanticSearch = new SemanticSearchOptions()
            {
                SemanticConfigurationName = "default",
                QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive),
                QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
                ErrorMode = SemanticErrorMode.Partial,
                MaxWait = TimeSpan.FromSeconds(5)
            }
        });

        var result = searchResponse.Value.GetResults().ToList();
        
        Log.Verbose("Number of search results: {Count}", result.Count);
        
        if (result.Count == 0)
        {
            return "No vehicles found with that name.";
        }

        var searchResultBuilder = new StringBuilder();

        searchResultBuilder.AppendLine("Here are the vehicles I found:");
        
        foreach (var resultPage in result)
        {
            Log.Verbose("Search result: {Summary}", resultPage.Document.summary);
            searchResultBuilder.AppendLine(resultPage.Document.summary);
        }

        return searchResultBuilder.ToString();
    }

    public class SwapiAzureAiSearchFunctionParameters
    {
        [JsonPropertyName("search_query")] 
        public string SearchQuery { get; set; }
    }

    private record VehicleSearchResult(
        string title,
        string summary,
        string model,
        string manufacturer
    );
}