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

namespace Demo9.Agents.PartSupplierAgent;

public class SearchIndexer
{
    private readonly Vectorizer _vectorizer;
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private const string IndexName = "supplier-parts-index";
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
                new SearchableField("part_number") { AnalyzerName = "keyword", IsFilterable = true },
                new SearchableField("part_name") { AnalyzerName = "en.microsoft" },
                new SearchableField("part_type") { AnalyzerName = "keyword", IsFilterable = true },
                new SearchableField("category") { AnalyzerName = "keyword", IsFilterable = true },
                new SearchableField("supplier") { AnalyzerName = "en.microsoft", IsFilterable = true },
                new SimpleField("unit_cost", SearchFieldDataType.Double) { IsFilterable = true, IsSortable = true },
                new SimpleField("lead_time_weeks", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchableField("country_of_origin") { AnalyzerName = "keyword", IsFilterable = true },
                new SimpleField("warranty_period_months", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchableField("stock_status") { AnalyzerName = "keyword", IsFilterable = true },
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
                            new SemanticField("part_name"),
                            new SemanticField("supplier")
                        },
                        KeywordsFields =
                        {
                            new SemanticField("part_type"),
                            new SemanticField("part_number"),
                            new SemanticField("category"),
                            new SemanticField("stock_status")
                        }
                    })
                }
            }
        };

        await _indexClient.CreateOrUpdateIndexAsync(index, true);
    }

    private async Task ImportCsvIntoSearch()
    {
        using var reader = new StreamReader("./Agents/PartSupplierAgent/supplier_parts.csv");
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
            double.TryParse(record.Unit_Cost?.ToString() ?? "0", out double unitCost);
            int.TryParse(record.Lead_time_weeks?.ToString() ?? "0", out int leadTimeWeeks);
            int.TryParse(record.Warranty_Period_months?.ToString() ?? "0", out int warrantyPeriodMonths);

            var supplierPartRecord = new SupplierPartRecord
            {
                PartNumber = record.Part_Number,
                PartName = record.Part_Name,
                PartType = record.Part_Type,
                Category = record.Category,
                Supplier = record.Supplier,
                UnitCost = unitCost,
                LeadTimeWeeks = leadTimeWeeks,
                CountryOfOrigin = record.Country_of_Origin,
                WarrantyPeriodMonths = warrantyPeriodMonths,
                StockStatus = record.Stock_Status
            };
            
            await _vectorizer.Vectorize(supplierPartRecord);

            var id = GenerateHashId(supplierPartRecord.Identifier);
            batch.Actions.Add(new IndexDocumentsAction<SearchDocument>(
                IndexActionType.MergeOrUpload,
                new SearchDocument
                {
                    ["id"] = id,
                    ["part_number"] = supplierPartRecord.PartNumber,
                    ["part_name"] = supplierPartRecord.PartName,
                    ["part_type"] = supplierPartRecord.PartType,
                    ["category"] = supplierPartRecord.Category,
                    ["supplier"] = supplierPartRecord.Supplier,
                    ["unit_cost"] = supplierPartRecord.UnitCost,
                    ["lead_time_weeks"] = supplierPartRecord.LeadTimeWeeks,
                    ["country_of_origin"] = supplierPartRecord.CountryOfOrigin,
                    ["warranty_period_months"] = supplierPartRecord.WarrantyPeriodMonths,
                    ["stock_status"] = supplierPartRecord.StockStatus,
                    ["summary_vector"] = supplierPartRecord.VectorizedSummary
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

    private class SupplierPartRecord : IVectorizedData
    {
        public string Supplier { get; set; }
        public string PartNumber { get; set; }
        public string PartName { get; set; }
        public string PartType { get; set; }
        public string Category { get; set; }
        public double UnitCost { get; set; }
        public int LeadTimeWeeks { get; set; }
        public string CountryOfOrigin { get; set; }
        public int WarrantyPeriodMonths { get; set; }
        public string StockStatus { get; set; }

        public string Identifier => $"{Supplier}-{PartNumber}";

        public string Summary =>
            $"{PartName} (Part Number: {PartNumber}) is a {PartType} in category {Category}, supplied by {Supplier}. Unit cost: {UnitCost:C}, lead time: {LeadTimeWeeks} weeks, warranty: {WarrantyPeriodMonths} months, origin: {CountryOfOrigin}, stock status: {StockStatus}.";

        public ReadOnlyMemory<float> VectorizedSummary { get; set; }
    }
}