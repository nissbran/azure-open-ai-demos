using System;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Embeddings;

namespace SwapiIndexer;

public class SwapiVehicleSearchIndexCreator
{
    private readonly SearchIndexClient _indexClient;
    private const string IndexName = "swapi-vehicle-index";
    private const string VectorConfigName = "summary-vector-config";
    private const string AlgorithmConfigName = "summary-alg-config";
    
    private const string VectorizerName = "openai";
    private const int ModelDimensions = 3072;
    private readonly string _embeddingModel;
    private readonly Uri _openAIResourceUri;
    private readonly string _openAIApiKey;

    public SwapiVehicleSearchIndexCreator(IConfiguration configuration)
    {
        _openAIResourceUri = new Uri(configuration["AzureOpenAI:Endpoint"]);
        _openAIApiKey = configuration["AzureOpenAI:ApiKey"];
        _embeddingModel = configuration["AzureOpenAI:EmbeddingModel"];
        
        var endpoint = new Uri(configuration["AzureAISearch:Endpoint"]);
        var credential = new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]);
        _indexClient = new SearchIndexClient(endpoint, credential);
    }

    public async Task CreateVectorIndexAsync()
    {
        var index = new SearchIndex(IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("title") { AnalyzerName = "en.microsoft" },
                new SearchableField("summary") { AnalyzerName = "en.microsoft" },
                new SearchableField("model") { AnalyzerName = "keyword", IsFilterable = true },
                new SearchableField("manufacturer") { AnalyzerName = "keyword", IsFilterable = true },
                new SearchableField("class") { AnalyzerName = "keyword", IsFilterable = true },
                new SearchField("summary_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = ModelDimensions,
                    VectorSearchProfileName = VectorConfigName
                },
            },
            VectorSearch = new VectorSearch
            {
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(AlgorithmConfigName)
                },
                Vectorizers =
                {
                    new AzureOpenAIVectorizer(VectorizerName)
                    {
                        Parameters = new AzureOpenAIVectorizerParameters()
                        {
                            ModelName = AzureOpenAIModelName.TextEmbedding3Large,
                            ResourceUri = _openAIResourceUri,
                            ApiKey = _openAIApiKey,
                            DeploymentName = _embeddingModel
                        }
                    }
                },
                Profiles =
                {
                    
                    new VectorSearchProfile(VectorConfigName, AlgorithmConfigName)
                    {
                        VectorizerName = VectorizerName
                    }
                }
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new SemanticPrioritizedFields()
                    {
                        ContentFields =
                        {
                            new SemanticField("title"),
                            new SemanticField("summary")
                        },
                        KeywordsFields =
                        {
                            new SemanticField("model"),
                            new SemanticField("manufacturer"),
                            new SemanticField("class")
                        }
                    })
                }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index);
    }
}