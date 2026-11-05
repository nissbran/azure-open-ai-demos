using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo3;

public class VehicleSearchFunction 
{
    private readonly SearchClient _searchClient;
    private const string FunctionName = "call_vehicle_search";

    public VehicleSearchFunction(IConfiguration configuration)
    {
        _searchClient = new SearchClient(new Uri(configuration["AzureAISearch:Endpoint"]), "swapi-vehicle-index", new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
    }

    public AITool GetFunctionDefinition()
    {
        return AIFunctionFactory.Create(GetVehicles, FunctionName);
    }

    [Description("Searches for a vehicle in Star Wars.")]
    public async Task<string> GetVehicles(SwapiAzureAiSearchFunctionParameters parameters)
    {
        Log.Verbose("Searching for vehicles with query {SearchQuery}", parameters.SearchQuery);
        
        var searchResponse = await _searchClient.SearchAsync<VehicleSearchResult>(parameters.SearchQuery, new SearchOptions()
        {
            QueryType = SearchQueryType.Full,
            Size = 5,
            VectorSearch = new VectorSearchOptions
            {
                Queries =
                {
                    new VectorizableTextQuery(parameters.SearchQuery)
                    {
                        KNearestNeighborsCount = 3,
                        Fields = { "summary_vector" }
                    }
                }
            },
            // SemanticSearch = new SemanticSearchOptions()
            // {
            //     SemanticConfigurationName = "default",
            //     QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive),
            //     QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
            //     ErrorMode = SemanticErrorMode.Partial,
            //     MaxWait = TimeSpan.FromSeconds(5)
            // }
        });

        var result = searchResponse.Value.GetResults().ToList();
        
        Log.Verbose("Number of search results: {Count}", result.Count);
        
        if (result.Count == 0)
        {
            return "[]";
        }
        
        var json = JsonSerializer.Serialize(result.Select(searchResult => new VehicleSearchResult(
            searchResult.Document.title,
            searchResult.Document.summary,
            searchResult.Document.model,
            searchResult.Document.manufacturer
        )).ToList());
            
        Log.Verbose("Search result: {Summary}", json);
        
        return json;
    }

    public class SwapiAzureAiSearchFunctionParameters
    {
        [JsonPropertyName("search_query")] 
        [Description("The search query for the vehicle, e.g. speeder bike")]
        public string SearchQuery { get; set; }
    }

    private record VehicleSearchResult(
        string title,
        string summary,
        string model,
        string manufacturer
    );
}