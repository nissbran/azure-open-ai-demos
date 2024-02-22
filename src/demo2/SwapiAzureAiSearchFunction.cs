using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo2;

public class SwapiAzureAiSearchFunction : IGptFunction
{
    private readonly SearchClient _searchClient;
    private readonly string _model;
    private readonly OpenAIClient _client;
    public const string FunctionName = "call_vehicle_search";

    public SwapiAzureAiSearchFunction(IConfiguration configuration)
    {
        _searchClient = new SearchClient(new Uri(configuration["AzureCognitiveSearch:Endpoint"]), "swapi-vehicle-index", new AzureKeyCredential(configuration["AzureCognitiveSearch:ApiKey"]));
        _model = configuration["AzureOpenAI:EmbeddingModel"];
        _client = new OpenAIClient(new Uri(configuration["AzureOpenAI:Endpoint"]), new AzureKeyCredential(configuration["AzureOpenAI:ApiKey"]));
    }

    public FunctionDefinition GetFunctionDefinition()
    {
        return new FunctionDefinition
        {
            Name = FunctionName,
            Description = "Searches for a vehicle in Star Wars.",
            Parameters = BinaryData.FromObjectAsJson(
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
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        };
    }

    public async Task<string> GetVehicles(SwapiAzureAiSearchFunctionParameters parameters)
    {
        Log.Information("Searching for vehicles with query {SearchQuery}", parameters.SearchQuery);
        
        var embeddingsResult = await _client.GetEmbeddingsAsync(new EmbeddingsOptions(_model, new []{parameters.SearchQuery}));

        var searchResponse = await _searchClient.SearchAsync<VehicleSearchResult>(parameters.SearchQuery, new SearchOptions()
        {
            QueryType = SearchQueryType.Semantic,
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizedQuery(embeddingsResult.Value.Data[0].Embedding)
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
        
        if (result.Count == 0)
        {
            return "No vehicles found with that name.";
        }

        var searchResultBuilder = new StringBuilder();

        searchResultBuilder.AppendLine("Here are the vehicles I found:");
        
        foreach (var resultPage in result)
        {
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