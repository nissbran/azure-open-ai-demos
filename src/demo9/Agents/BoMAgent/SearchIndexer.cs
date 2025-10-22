using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Demo9.Indexers;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo9.Agents.BoMAgent;

public class SearchIndexer
{
    private readonly Vectorizer _vectorizer;
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private const string IndexName = "bill-of-materials-index";
    private const string VectorConfigName = "summary-vector-config";
    private const string AlgorithmConfigName = "summary-alg-config";
    
    private const string VectorizerName = "openai";
    private const int LargeModelDimensions = 3072;
    private const int SmallModelDimensions = 1536;
    private readonly int _modelDimensions;
    private readonly string _embeddingModel;
    private readonly Uri _openAIResourceUri;
    private readonly string _openAIApiKey;

    public SearchIndexer(IConfiguration configuration)
    {
        _vectorizer = new Vectorizer(configuration);
        _openAIResourceUri = new Uri(configuration["AzureOpenAI:Endpoint"]);
        _openAIApiKey = configuration["AzureOpenAI:ApiKey"];
        _embeddingModel = configuration["AzureOpenAI:EmbeddingModel"];
        
        _modelDimensions = _embeddingModel == AzureOpenAIModelName.TextEmbedding3Large ? LargeModelDimensions : SmallModelDimensions;
        
        var endpoint = new Uri(configuration["AzureAISearch:Endpoint"]);
        var credential = new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]);
        _indexClient = new SearchIndexClient(endpoint, credential);
        _searchClient = new SearchClient(endpoint, IndexName, credential);
    }
    
    public async Task CreateIndexAsync()
    {
        Log.Information("Creating or updating the index {IndexName}", IndexName);
        await CreateVectorIndexAsync();
        await ImportCsvIntoSearch();
    }

    private async Task CreateVectorIndexAsync()
    {
        var index = new SearchIndex(IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("brand") { IsFilterable = true, IsSortable = true },
                new SearchableField("part_number") { IsFilterable = true, IsSortable = true },
                new SearchableField("truck_model") { IsFilterable = true, IsSortable = true},
                new SearchableField("part_name") { IsSortable = true, IsFilterable = true },
                new SearchableField("part_type") { IsSortable = true, IsFilterable = true },
                new SimpleField("quantity", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchField("summary_vector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _modelDimensions,
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
                            ModelName = _embeddingModel,
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
                        },
                        KeywordsFields =
                        {
                            new SemanticField("truck_model"),
                            new SemanticField("part_number"),
                            new SemanticField("part_type"),
                            new SemanticField("part_name"),
                            new SemanticField("brand"),
                        }
                    })
                }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index, true);
    }

    private async Task ImportCsvIntoSearch()
    {
        using var reader = new StreamReader("./Agents/BoMAgent/bill_of_materials.csv");
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<dynamic>();
        
        var iteration = 0;
        var batch = new IndexDocumentsBatch<SearchDocument>();
        
        foreach (var record in records)
        {
            int.TryParse(record.Quantity ?? record.quantity ?? "0", out int quantity);
            var bomRecord = new BoMRecord
            {
                Brand = record.Brand ?? record.brand ?? "",
                TruckModel = record.Truck_Model ?? record.truck_model ?? "",
                PartNumber = record.Part_Number ?? record.part_number ?? "",
                PartName = record.Part_Name ?? record.part_name ?? "",
                PartType = record.Part_Type ?? record.part_type ?? "",
                Quantity = quantity
            };
            await _vectorizer.Vectorize(bomRecord);
            
            var id = GenerateHashId(bomRecord.Identifier);
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = id,
                    ["brand"] = bomRecord.Brand,
                    ["truck_model"] = bomRecord.TruckModel,
                    ["part_number"] = bomRecord.PartNumber,
                    ["part_name"] = bomRecord.PartName,
                    ["part_type"] = bomRecord.PartType,
                    ["quantity"] = bomRecord.Quantity,
                    ["summary_vector"] = bomRecord.VectorizedSummary
                }
            ));
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
            Log.Information("Indexed {Succeeded} bill of materials", succeeded);
        }
    }

    private static string GenerateHashId(string text)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }
    
    private class BoMRecord : IVectorizedData
    {
        public string Brand { get; set; }
        public string TruckModel { get; set; }
        public string PartNumber { get; set; }
        public string PartName { get; set; }
        public string PartType { get; set; }
        public int Quantity { get; set; }

        public string Identifier => $"{TruckModel}-{PartNumber}";

        public string Summary =>
            $"{PartName} (Part Number: {PartNumber}) is a {PartType} used in the {TruckModel} truck. Quantity required: {Quantity}.";
        public ReadOnlyMemory<float> VectorizedSummary { get; set; }
    }
}