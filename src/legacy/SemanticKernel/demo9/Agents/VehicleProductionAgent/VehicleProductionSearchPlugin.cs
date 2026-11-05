using System;
using System.Collections.Generic;
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

namespace Demo9.Agents.VehicleProductionAgent;

public class VehicleProductionSearchPlugin
{
    private readonly SearchClient _searchClient;

    public VehicleProductionSearchPlugin(IConfiguration configuration)
    {
        _searchClient = new SearchClient(
            new Uri(configuration["AzureAISearch:Endpoint"]), 
            "vehicle-production-index", 
            new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
    }
    
    [KernelFunction("call_vehicle_production_search")]
    [Description("Searches vehicle production data including VIN numbers, build dates, models, and production status")]
    [return: Description("An json array of objects containing information about vehicle production that match the search query.")]
    public async Task<List<VehicleProductionSearchResult>> GetVehicleProductionData(SearchParameters parameters)
    {
        try
        {
            Log.Verbose("Searching for vehicle production data with query {SearchQuery}", parameters.SearchQuery);
            
            var searchResponse = await _searchClient.SearchAsync<VehicleProductionSearchResult>(parameters.SearchQuery, new SearchOptions()
            {
                QueryType = SearchQueryType.Semantic,
                Size = 5,
                VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizableTextQuery(parameters.SearchQuery)
                        {
                            KNearestNeighborsCount = 5,
                            Fields = { "summary_vector" }
                        }
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
                return [];
            }
            
            var json = JsonSerializer.Serialize(result.Select(searchResult => searchResult.Document).ToList());
            
            Log.Verbose("Search result: {Summary}", json);
            
            return result.Select(searchResult => searchResult.Document).ToList();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to search for vehicle production data");
            throw;
        }
    }

    public class SearchParameters
    {
        [JsonPropertyName("search_query")] 
        [Description("The search query to find relevant vehicle production information including VIN numbers, models, build dates, production status, and plant locations.")]
        public string SearchQuery { get; set; }
    }

    public record VehicleProductionSearchResult(
        string vin,
        string model,
        string build_date,
        string production_line,
        string status,
        string plant_location,
        string engine_type,
        string color,
        string options
    );
}