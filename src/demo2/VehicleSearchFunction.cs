﻿using System;
using System.ComponentModel;
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
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Serilog;

namespace Demo2;

public class VehicleSearchFunction : IGptFunction
{
    private readonly SearchClient _searchClient;
    private readonly string _model;
    private readonly OpenAIClient _client;
    private readonly EmbeddingClient _embeddingClient;
    public const string FunctionName = "call_vehicle_search";

    public VehicleSearchFunction(IConfiguration configuration)
    {
        _searchClient = new SearchClient(new Uri(configuration["AzureAISearch:Endpoint"]), "swapi-vehicle-index", new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
        _model = configuration["AzureOpenAI:EmbeddingModel"];
        _client = new AzureOpenAIClient(new Uri(configuration["AzureOpenAI:Endpoint"]), new AzureKeyCredential(configuration["AzureOpenAI:ApiKey"]));
        _embeddingClient = _client.GetEmbeddingClient(_model);
    }

    public ChatTool GetToolDefinition()
    {
        return ChatTool.CreateFunctionTool(FunctionName, "Searches for a vehicle in Star Wars.", GetFunctionParameters());
    }

    private BinaryData GetFunctionParameters()
    {
        return BinaryData.FromObjectAsJson(new
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
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public async Task<string> GetVehicles(SwapiAzureAiSearchFunctionParameters parameters)
    {
        Log.Information("Searching for vehicles with query {SearchQuery}", parameters.SearchQuery);
        
        var embeddingsResult = await _embeddingClient.GenerateEmbeddingAsync(parameters.SearchQuery);

        var searchResponse = await _searchClient.SearchAsync<VehicleSearchResult>(parameters.SearchQuery, new SearchOptions()
        {
            QueryType = SearchQueryType.Semantic,
            Size = 5,
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