using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace SwapiIndexer;

public class VehicleVectorizer
{
    private readonly OpenAIClient _client;
    private readonly string _model;
    private long _usedTokens;

    public VehicleVectorizer(IConfiguration configuration)
    {
        _model = configuration["AzureOpenAI:EmbeddingModel"];
        _client = new OpenAIClient(
            new Uri(configuration["AzureOpenAI:Endpoint"]),
            new AzureKeyCredential(configuration["AzureOpenAI:ApiKey"]));
    }

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddings(string text)
    {
        var response = await _client.GetEmbeddingsAsync(new EmbeddingsOptions(_model, new []{text}));
    
        _usedTokens = response.Value.Usage.TotalTokens;
        return response.Value.Data.First().Embedding;
    }
    
    public async Task Vectorize(List<Vehicle> vehicles)
    {
        foreach (var vehicle in vehicles)
        {
            Log.Information("Starting to vectorizing vehicle {Vehicle}", vehicle.Name);
    
            var embeddings = await GenerateEmbeddings(vehicle.Summary);
            
            vehicle.VectorizedSummary = embeddings;
        }
    
        Log.Information("All vehicles vectorized, used {UsedTokens} tokens", _usedTokens);
    }
    
}