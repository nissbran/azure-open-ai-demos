using System.ComponentModel;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ModelContextProtocol.Server;

namespace McpToolServer.Tools;

[McpServerToolType]
public sealed class VehicleSearchTool
{
    [McpServerTool, Description("Searches for a vehicle in Star Wars.")]
    public static async Task<string> SearchVehicles(
        SearchClient searchClient,
        ILogger<VehicleSearchTool> logger,
        [Description("The search query for the vehicle, e.g. speeder bike")]
        string searchQuery)
    {
        logger.LogInformation("Searching for vehicles with query {SearchQuery}", searchQuery);
        var searchResponse = await searchClient.SearchAsync<VehicleSearchResult>(searchQuery, new SearchOptions()
        {
            QueryType = SearchQueryType.Full,
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
            }
        });

        var result = searchResponse.Value.GetResults().ToList();

        logger.LogInformation("Number of search results: {Count}", result.Count);

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

        logger.LogInformation("Search result: {Summary}", json);

        return json;
    }

    private record VehicleSearchResult(
        string title,
        string summary,
        string model,
        string manufacturer
    );
}