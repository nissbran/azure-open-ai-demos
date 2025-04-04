using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Serilog;

namespace Demo5.Agents.Vehicle;

public class VehicleAzureAiSearchPlugin
{
    private readonly SearchClient _searchClient;

    public VehicleAzureAiSearchPlugin(IConfiguration configuration)
    {
        _searchClient = new SearchClient(new Uri(configuration["AzureAISearch:Endpoint"]), "swapi-vehicle-index", new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
    }
    
    [KernelFunction("call_vehicle_search")]
    [Description("Searches for a vehicle in Star Wars.")]
    [return: Description("An json array of vehicles")]
    public async Task<string> GetVehicles(SwapiAzureAiSearchFunctionParameters parameters)
    {
        try
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
        catch (Exception e)
        {
            Log.Error(e, "Failed to search for vehicles");
            throw;
        }
    }

    public class SwapiAzureAiSearchFunctionParameters
    {
        [JsonPropertyName("search_query")] 
        [Description("The search query for the vehicle, e.g. speeder bike")]
        public string SearchQuery { get; set; }
    }

    public record VehicleSearchResult(
        string title,
        string summary,
        string model,
        string manufacturer
    );
}