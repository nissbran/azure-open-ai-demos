using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;

namespace MaintenanceManualsIndexer;

/// <summary>
/// Creates the parent and child content search indexes.
/// </summary>
public class SearchIndexCreator
{
    internal const string ChildIndexName = "maintenance-manual-chunks";
    internal const string ParentIndexName = "maintenance-manuals";
    internal const int VectorDimensions = 1536;

    private const string VectorConfigName = "hnsw";
    private const string VectorizerName = "azure-openai";
    private const string SemanticConfigName = "default";

    private readonly string _embeddingModel;
    private readonly SearchIndexClient _indexClient;
    private readonly Uri _openAIResourceUri;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexCreator"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="indexClient">The search index client.</param>
    public SearchIndexCreator(IConfiguration configuration, SearchIndexClient indexClient)
    {
        _indexClient = indexClient;
        _embeddingModel = configuration.GetRequiredValue("AzureOpenAI:EmbeddingModel");
        _openAIResourceUri = new Uri(configuration.GetRequiredValue("AzureOpenAI:Endpoint"));
    }

    /// <summary>
    /// Creates or updates the parent and child indexes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing creation of both indexes.</returns>
    public async Task CreateVectorIndexAsync(CancellationToken cancellationToken = default)
    {
        var parentIndex = new SearchIndex(ParentIndexName)
        {
            Fields =
            {
                new SearchField("parent_id", SearchFieldDataType.String)
                {
                    IsKey = true,
                    IsFilterable = true
                },
                new SearchField("title", SearchFieldDataType.String)
                {
                    IsSearchable = true,
                    IsFilterable = true,
                    IsHidden = false,
                    IsSortable = true,
                    AnalyzerName = LexicalAnalyzerName.EnMicrosoft
                },
                new SearchField("abstract", SearchFieldDataType.String)
                {
                    IsSearchable = true,
                    IsHidden = false,
                    AnalyzerName = LexicalAnalyzerName.EnMicrosoft
                }
            },
            SemanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = SemanticConfigName,
                Configurations =
                {
                    new SemanticConfiguration(SemanticConfigName, new SemanticPrioritizedFields
                    {
                        TitleField = new SemanticField("title"),
                        ContentFields =
                        {
                            new SemanticField("abstract")
                        }
                    })
                }
            }
        };

        var childIndex = new SearchIndex(ChildIndexName)
        {
            Fields =
            {
                new SearchField("chunk_id", SearchFieldDataType.String)
                {
                    IsKey = true,
                    IsSearchable = true,
                    IsFilterable = true,
                    AnalyzerName = LexicalAnalyzerName.Keyword
                },
                new SearchField("parent_id", SearchFieldDataType.String)
                {
                    IsFilterable = true
                },
                new SearchField("chunk", SearchFieldDataType.String)
                {
                    IsSearchable = true,
                    IsHidden = false,
                    AnalyzerName = LexicalAnalyzerName.EnMicrosoft
                },
                new SearchField("chunk_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    IsHidden = true,
                    IsStored = false,
                    VectorSearchDimensions = VectorDimensions,
                    VectorSearchProfileName = VectorConfigName
                }
            },
            VectorSearch = new VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile(VectorConfigName, VectorConfigName)
                    {
                        VectorizerName = VectorizerName
                    }
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(VectorConfigName)
                },
                Vectorizers =
                {
                    new AzureOpenAIVectorizer(VectorizerName)
                    {
                        Parameters = new AzureOpenAIVectorizerParameters
                        {
                            ModelName = AzureOpenAIModelName.TextEmbedding3Small,
                            ResourceUri = _openAIResourceUri,
                            DeploymentName = _embeddingModel
                        }
                    }
                }
            },
            SemanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = SemanticConfigName,
                Configurations =
                {
                    new SemanticConfiguration(SemanticConfigName, new SemanticPrioritizedFields
                    {
                        ContentFields =
                        {
                            new SemanticField("chunk")
                        }
                    })
                }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(parentIndex, cancellationToken: cancellationToken);
        await _indexClient.CreateOrUpdateIndexAsync(childIndex, cancellationToken: cancellationToken);
    }
}
