using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;
using Serilog;

namespace Demo9.Indexers;

public class Vectorizer
{
    private readonly OpenAIClient _client;
    private readonly EmbeddingClient _embeddingClient;
    private readonly string _model;
    private long _usedTokens;

    public Vectorizer(IConfiguration configuration)
    {
        _model = configuration["AzureOpenAI:EmbeddingModel"];
        _client = new AzureOpenAIClient(
            new Uri(configuration["AzureOpenAI:Endpoint"]),
            new AzureKeyCredential(configuration["AzureOpenAI:ApiKey"]));
        _embeddingClient = _client.GetEmbeddingClient(_model);
    }

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddings(string text)
    {
        var response = await _embeddingClient.GenerateEmbeddingAsync(text);
        
        return response.Value.ToFloats();
    }
    
    public async Task Vectorize(IVectorizedData vectorizedData)
    {
        Log.Information("Starting to vectorizing data with identifier {Identifier}", vectorizedData.Identifier);

        var embeddings = await GenerateEmbeddings(vectorizedData.Summary);
        
        vectorizedData.VectorizedSummary = embeddings;

        Log.Information("Data vectorized, used {UsedTokens} tokens", _usedTokens);
    }
    
    public async Task VectorizeList(List<IVectorizedData> vectorizedData)
    {
        foreach (var data in vectorizedData)
        {
            Log.Information("Starting to vectorizing data with identifier {Identifier}", data.Identifier);
    
            var embeddings = await GenerateEmbeddings(data.Summary);
            
            data.VectorizedSummary = embeddings;
        }
    
        Log.Information("All data vectorized, used {UsedTokens} tokens", _usedTokens);
    }
}