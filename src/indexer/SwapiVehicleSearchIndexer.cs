using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SwapiIndexer;

public class SwapiVehicleSearchIndexer
{
    private readonly SearchClient _searchClient;

    private const string IndexName = "swapi-vehicle-index";

    public SwapiVehicleSearchIndexer(IConfiguration configuration)
    {
        var endpoint = new Uri(configuration["AzureCognitiveSearch:Endpoint"]);
        var credential = new AzureKeyCredential(configuration["AzureCognitiveSearch:ApiKey"]);
        _searchClient = new SearchClient(endpoint, IndexName, credential);
    }

    public async Task ImportIntoSearch(List<Vehicle> vehicles)
    {
        Log.Information("Importing vehicle into search");
        await InsertDocumentsAsync(vehicles);
    }

    private async Task InsertDocumentsAsync(List<Vehicle> vehicles)
    {
        var iteration = 0;
        var batch = new IndexDocumentsBatch<SearchDocument>();
        foreach (var vehicle in vehicles)
        {
            var id = GenerateHashId($"{vehicle.Name}");
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = id,
                    ["title"] = vehicle.Name,
                    ["summary"] = vehicle.Summary,
                    ["summary_vector"] = vehicle.VectorizedSummary,
                    ["model"] = vehicle.Model,
                    ["manufacturer"] = vehicle.Manufacturer,
                    ["class"] = vehicle.VehicleClass
                }));
        }

        iteration++;
        if (iteration % 1_000 is 0)
        {
            // Every one thousand documents, batch create.
            IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);

            Log.Information("Indexed {Succeeded} documents", succeeded);

            batch = new();
        }

        if (batch is { Actions.Count: > 0 })
        {
            IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            Log.Information("Indexed {Succeeded} vehicles", succeeded);
        }
    }

    private static string GenerateHashId(string text)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }
}