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

namespace Demo9.Agents.BoMAgent;

public class BillOfMaterialsSearchPlugin
{
    private readonly SearchClient _searchClient;

    public BillOfMaterialsSearchPlugin(IConfiguration configuration)
    {
        _searchClient = new SearchClient(
            new Uri(configuration["AzureAISearch:Endpoint"]), 
            "bill-of-materials-index", 
            new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
    }
    
    [KernelFunction("call_bom_search")]
    [Description("Searches bill of material (BoM) for all vehicles")]
    [return: Description("An json array of objects containing information about the bill of materials for vehicles that match the search query.")]
    public async Task<List<BillOfMaterialsSearchResult>> GetBoMForVehicles(SearchParameters parameters)
    {
        try
        {
            Log.Verbose("Searching for vehicles with query {SearchQuery}", parameters.SearchQuery);
            
            var searchResponse = await _searchClient.SearchAsync<BillOfMaterialsSearchResult>(parameters.SearchQuery, new SearchOptions()
            {
                QueryType = SearchQueryType.Semantic,
                Size = 10,
                VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizableTextQuery(parameters.SearchQuery)
                        {
                            KNearestNeighborsCount = 10,
                            Weight = 0.5f,
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
            Log.Error(e, "Failed to search for bill of materials");
            throw;
        }
    }

    [KernelFunction("find_bill_of_materials_for_model")]
    [Description("Get bill of material (BoM) for a model")]
    [return: Description("An json array of objects containing information about the bill of materials for")]
    public async Task<List<BillOfMaterialsSearchResult>> GetBillOfMaterialsForModel([Description("The truckt model name")]string truckModel)
    {
        try
        {
            Log.Verbose("Searching for model with parameter with query {Model}", truckModel);
            
            var searchResponse = await _searchClient.SearchAsync<BillOfMaterialsSearchResult>(truckModel, new SearchOptions()
            {
                QueryType = SearchQueryType.Simple,
                Size = 20
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
            Log.Error(e, "Failed to find for bill of materials for model");
            throw;
        }
    }
    
    public class SearchParameters
    {
        [JsonPropertyName("search_query")] 
        [Description("The search query to find relevant bill of materials for trucks.")]
        public string SearchQuery { get; set; }
    }

    public record BillOfMaterialsSearchResult(
        string truck_model,
        string part_number,
        string part_name,
        string part_type,
        int quantity
    );
}