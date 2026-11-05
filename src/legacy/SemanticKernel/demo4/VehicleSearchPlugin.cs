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

namespace Demo4;

public class VehicleSearchPlugin
{
    private readonly SearchClient _searchClient;

    public VehicleSearchPlugin(IConfiguration configuration)
    {
        _searchClient = new SearchClient(new Uri(configuration["AzureAISearch:Endpoint"]), "swapi-vehicle-index", new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
    }
    
    [KernelFunction("call_vehicle_search")]
    [Description("Searches for a vehicle in Star Wars.")]
    [return: Description("An array of vehicles")]
    public async Task<string> GetVehicles(SwapiAzureAiSearchFunctionParameters parameters)
    {
        try
        {
            Log.Verbose("Searching for vehicles with query {SearchQuery}", parameters.SearchQuery);
        
            //var embeddingsResult = await _embeddingClient.GenerateEmbeddingAsync(parameters.SearchQuery);

            //Log.Verbose("Embeddings result: {EmbeddingsResult}", embeddingsResult.Value.Vector);
            
            var searchResponse = await _searchClient.SearchAsync<VehicleSearchResult>(parameters.SearchQuery, new SearchOptions()
            {
                QueryType = SearchQueryType.Full,
                VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizableTextQuery(parameters.SearchQuery)
                        {
                            KNearestNeighborsCount = 3,
                            Fields = { "summary_vector" }
                        }
                        // new VectorizedQuery(embeddingsResult.Value.Vector)
                        // {
                        //     KNearestNeighborsCount = 3,
                        //     Fields = { "summary_vector" }
                        // },
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

    private record VehicleSearchResult(
        string title,
        string summary,
        string model,
        string manufacturer
    );
}