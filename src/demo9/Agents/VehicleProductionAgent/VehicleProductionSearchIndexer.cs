using System;
using System.Collections.Generic;
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

namespace Demo9.Agents.VehicleProductionAgent;

public class VehicleProductionSearchIndexer
{
    private readonly Vectorizer _vectorizer;
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private const string IndexName = "vehicle-production-index";
    private const string VectorConfigName = "summary-vector-config";
    private const string AlgorithmConfigName = "summary-alg-config";

    private const string VectorizerName = "openai";
    private const int LargeModelDimensions = 3072;
    private const int SmallModelDimensions = 1536;
    private readonly int _modelDimensions;
    private readonly string _embeddingModel;
    private readonly Uri _openAIResourceUri;
    private readonly string _openAIApiKey;

    public VehicleProductionSearchIndexer(IConfiguration configuration)
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
        try
        {
            Log.Information("Deleting existing vehicle production index if it exists...");
            await _indexClient.DeleteIndexAsync(IndexName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            Log.Information("Index did not exist, continuing...");
        }

        Log.Information("Creating vehicle production search index...");

        var index = new SearchIndex(IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SearchableField("vin") { IsFilterable = true, IsSortable = true },
                new SearchableField("model") { IsFilterable = true, IsFacetable = true },
                new SimpleField("build_date", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SearchableField("production_line") { IsFilterable = true, IsFacetable = true },
                new SearchableField("status") { IsFilterable = true, IsFacetable = true },
                new SearchableField("plant_location") { IsFilterable = true, IsFacetable = true },
                new SearchableField("engine_type") { IsFilterable = true, IsFacetable = true },
                new SearchableField("color") { IsFilterable = true, IsFacetable = true },
                new SearchableField("options"),
                new SearchableField("summary"),
                new VectorSearchField("summary_vector", _modelDimensions, VectorConfigName)
            },
            SemanticSearch = new SemanticSearch
            {
                Configurations =
                {
                    new SemanticConfiguration("default", new SemanticPrioritizedFields
                    {
                        ContentFields =
                        {
                            new SemanticField("summary")
                        },
                        KeywordsFields =
                        {
                            new SemanticField("vin"),
                            new SemanticField("model"),
                            new SemanticField("status")
                        }
                    })
                }
            },
            VectorSearch = new VectorSearch
            {
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
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(AlgorithmConfigName)
                }
            }
        };

        await _indexClient.CreateIndexAsync(index);
    }

    private async Task ImportCsvIntoSearch()
    {
         using var reader = new StreamReader("./Agents/VehicleProductionAgent/vehicle_production_data.csv");
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
            var vehicleProductionData = new VehicleProductionData
            {
                Identifier = record.VIN,
                Vin = record.VIN,
                Model = record.Model,
                BuildDate = DateTimeOffset.Parse(record.Build_Date),
                ProductionLine = record.Production_Line,
                Status = record.Status,
                PlantLocation = record.Plant_Location,
                EngineType = record.Engine_Type,
                Color = record.Color,
                Options = record.Options,
                Summary = $"Vehicle VIN {record.VIN} is a {record.Model} built on {record.Build_Date} at {record.Plant_Location} on {record.Production_Line}. Current status: {record.Status}. Engine: {record.Engine_Type}. Color: {record.Color}. Options: {record.Options}",
            };
            
            await _vectorizer.Vectorize(vehicleProductionData);

            var id = GenerateHashId(record.VIN);
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = id,
                    ["vin"] = vehicleProductionData.Vin,
                    ["model"] = vehicleProductionData.Model,
                    ["build_date"] = vehicleProductionData.BuildDate.ToString("yyyy-MM-dd"),
                    ["production_line"] = vehicleProductionData.ProductionLine,
                    ["status"] = vehicleProductionData.Status,
                    ["plant_location"] = vehicleProductionData.PlantLocation,
                    ["engine_type"] = vehicleProductionData.EngineType,
                    ["color"] = vehicleProductionData.Color,
                    ["options"] = vehicleProductionData.Options,
                    ["summary"] = vehicleProductionData.Summary,
                    ["summary_vector"] = vehicleProductionData.VectorizedSummary
                }
            ));

            iteration++;
            if (iteration % 1_000 is 0)
            {
                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
                int succeeded = result.Results.Count(r => r.Succeeded);
                Log.Information("Indexed {Succeeded} documents", succeeded);
                batch = new();
            }
        }

        if (batch is { Actions.Count: > 0 })
        {
            IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
            int succeeded = result.Results.Count(r => r.Succeeded);
            Log.Information("Indexed {Succeeded} parts", succeeded);
        }
    }

    private static string GenerateHashId(string text)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }
}

public class VehicleProductionData : IVectorizedData
{
    public string Identifier { get; set; }
    public string Summary { get; set; }
    public ReadOnlyMemory<float> VectorizedSummary { get; set; }

    public string Vin { get; set; }
    public string Model { get; set; }
    public DateTimeOffset BuildDate { get; set; }
    public string ProductionLine { get; set; }
    public string Status { get; set; }
    public string PlantLocation { get; set; }
    public string EngineType { get; set; }
    public string Color { get; set; }
    public string Options { get; set; }
}