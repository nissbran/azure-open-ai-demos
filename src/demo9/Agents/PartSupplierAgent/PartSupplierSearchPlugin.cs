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

namespace Demo9.Agents.PartSupplierAgent;

public class PartSupplierSearchPlugin
{
    private readonly SearchClient _searchClient;

    public PartSupplierSearchPlugin(IConfiguration configuration)
    {
        _searchClient = new SearchClient(
            new Uri(configuration["AzureAISearch:Endpoint"]), 
            "supplier-parts-index", 
            new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
    }
    
    [KernelFunction]
    [Description("Search supplier parts. It does not contain what parts goes where. The data contains part number, part name, part type, category, supplier, unit cost, lead time in weeks, country of origin, warranty period in months, and stock status.")]
    public async Task<List<SupplierPartSearchResult>> SearchForSupplierParts(
        [Description("Seach query for supplier parts")]string searchQuery)
    {
        try
        {
            Log.Verbose("Searching for parts with query {SearchQuery}", searchQuery);
            
            var searchResponse = await _searchClient.SearchAsync<SupplierPartSearchResult>(searchQuery, new SearchOptions()
            {
                QueryType = SearchQueryType.Semantic,
                Size = 5,
                VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizableTextQuery(searchQuery)
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
            Log.Error(e, "Failed to search for bill of materials");
            throw;
        }
    }
    
    [KernelFunction]
    [Description("Get supplier part by part number")]
    public async Task<List<SupplierPartSearchResult>> GetSupplierPart(
        [Description("The part number or id")]string partNumber)
    {
        try
        {
            Log.Verbose("Searching for model with parameter with query {Model}", partNumber);
            
            var searchResponse = await _searchClient.SearchAsync<SupplierPartSearchResult>(partNumber, new SearchOptions()
            {
                QueryType = SearchQueryType.Simple,
                Size = 10
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
    
    public record SupplierPartSearchResult(
        string part_number,
        string part_name,
        string part_type,
        string category,
        string supplier,
        decimal unit_cost,
        int lead_time_weeks,
        string country_of_origin,
        int warranty_period_months,
        string stock_status
    );
}