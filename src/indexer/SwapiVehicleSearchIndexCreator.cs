using System;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;

namespace SwapiIndexer;

public class SwapiVehicleSearchIndexCreator
{
    private readonly SearchIndexClient _indexClient;
    private const string IndexName = "swapi-vehicle-index";
    private const string VectorConfigName = "summary-vector-config";
    private const string AlgorithmConfigName = "summary-alg-config";
    private const int ModelDimensions = 3072;

    public SwapiVehicleSearchIndexCreator(IConfiguration configuration)
    {
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
                Profiles =
                {
                    new VectorSearchProfile(VectorConfigName, AlgorithmConfigName)
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