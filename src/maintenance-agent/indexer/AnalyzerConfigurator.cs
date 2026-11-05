using Azure;
using Azure.AI.ContentUnderstanding;
using Microsoft.Extensions.Configuration;

namespace MaintenanceManualsIndexer;

public class AnalyzerConfigurator(ContentUnderstandingClient client, IConfiguration configuration)
{
    public async Task<string> CreateAnalyzerAsync(CancellationToken cancellationToken = default)
    {
        var analyzerId = configuration["ContentUnderstanding:ManualAnalyzerId"] ?? "ManualAnalyzerV1";
        ArgumentException.ThrowIfNullOrWhiteSpace(analyzerId);
        var fieldSchema = new ContentFieldSchema(
            new Dictionary<string, ContentFieldDefinition>
            {
                ["devices"] = new()
                {
                    Type = ContentFieldType.String,
                    Method = GenerationMethod.Generate,
                    Description = "Name of the device"
                },
                ["abstract_summary"] = new()
                {
                    Type = ContentFieldType.String,
                    Method = GenerationMethod.Generate,
                    Description = "Summarize the entire maintenance manual, identifying the devices, repair procedures, troubleshooting topics, and safety precautions covered. Use only information present in the document."
                }
            })
        {
            Name = "device_schema",
            Description = "Schema for extracting device information"
        };

        var analyzer = new ContentAnalyzer
        {
            BaseAnalyzerId = "prebuilt-document",
            Description = "Analyzer with semantic chunking",
            Config = new ContentAnalyzerConfig
            {
                ShouldReturnDetails = true,
                EnableLayout = true,
                ChunkingStrategy = new SemanticChunkingStrategy
                {
                    MaxTokens = 1000
                }
            },
            FieldSchema = fieldSchema
        };

        var completionModel = configuration["ContentUnderstanding:CompletionModel"] ?? "gpt-5.2";
        ArgumentException.ThrowIfNullOrWhiteSpace(completionModel);
        analyzer.Models["completion"] = completionModel;

        await client.CreateAnalyzerAsync(
            WaitUntil.Completed,
            analyzerId,
            analyzer,
            allowReplace: true,
            cancellationToken: cancellationToken);
        return analyzerId;
    }
}