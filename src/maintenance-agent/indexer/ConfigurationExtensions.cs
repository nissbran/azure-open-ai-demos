using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceManualsIndexer;

internal static class ConfigurationExtensions
{
    internal static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{key} is not configured.");

        return value;
    }
    

    internal static void ConfigureExtractor(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["PdfExtraction:Provider"] ?? "DocumentIntelligence";
        switch (provider.ToUpperInvariant())
        {
            case "DOCUMENTINTELLIGENCE":
                {
                    var endpoint = new Uri(configuration.GetRequiredValue("DocumentIntelligence:Endpoint"));
                    var apiKey = configuration["DocumentIntelligence:ApiKey"];
                    var client = string.IsNullOrWhiteSpace(apiKey)
                        ? new DocumentIntelligenceClient(endpoint, new AzureCliCredential())
                        : new DocumentIntelligenceClient(endpoint, new AzureKeyCredential(apiKey));
                    services.AddSingleton(client);
                    services.AddSingleton<IPdfTextExtractor>(p => new DocumentIntelligencePdfTextExtractor(p.GetRequiredService<DocumentIntelligenceClient>()));
                }
                break;
            case "CONTENTUNDERSTANDING":
                {
                    var endpoint = new Uri(configuration.GetRequiredValue("ContentUnderstanding:Endpoint"));
                    var apiKey = configuration["ContentUnderstanding:ApiKey"];
                    var options = new ContentUnderstandingClientOptions(
                        ContentUnderstandingClientOptions.ServiceVersion.V2026_06_01_Preview);
                    var client = string.IsNullOrWhiteSpace(apiKey)
                        ? new ContentUnderstandingClient(endpoint, new AzureCliCredential(), options)
                        : new ContentUnderstandingClient(endpoint, new AzureKeyCredential(apiKey), options);
                    services.AddSingleton(client);
                    services.AddSingleton<AnalyzerConfigurator>();
                    var analyzerId = configuration["ContentUnderstanding:ManualAnalyzerId"] ?? "ManualAnalyzerV1";
                    services.AddSingleton<IPdfTextExtractor>(p => new ContentUnderstandingPdfTextExtractor(
                        p.GetRequiredService<ContentUnderstandingClient>(), analyzerId));
                }
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported PdfExtraction:Provider '{provider}'. Use DocumentIntelligence or ContentUnderstanding.");
        }
    }

    internal static void ConfigureOpenAI(this IServiceCollection services, IConfiguration configuration)
    {
        var openAIEndpoint = new Uri(configuration.GetRequiredValue("AzureOpenAI:Endpoint"));
        var openAIClient = new AzureOpenAIClient(openAIEndpoint, new AzureCliCredential());
        services.AddSingleton(openAIClient.GetEmbeddingClient(configuration.GetRequiredValue("AzureOpenAI:EmbeddingModel")));
    }
}
