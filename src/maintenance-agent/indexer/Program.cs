using Azure.Identity;
using Azure.Search.Documents.Indexes;
using MaintenanceManualsIndexer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

DotNetEnv.Env.TraversePath().Load();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateLogger();

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

// try
// {
    var files = ManualFileDiscovery.Find(args);
    var builder = Host.CreateApplicationBuilder();
    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", false)
        .AddJsonFile("appsettings.local.json", true)
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables();

    var configuration = builder.Configuration;

    builder.Services.AddSerilog();
    builder.Services.AddSingleton(new SearchIndexClient(
        new Uri(configuration.GetRequiredValue("AzureAISearch:Endpoint")), new AzureCliCredential()));
    builder.Services.ConfigureExtractor(configuration);
    builder.Services.ConfigureOpenAI(configuration);
    builder.Services.AddSingleton<SearchIndexCreator>();
    builder.Services.AddSingleton<PdfDocumentIndexer>();

    using var host = builder.Build();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Found {Count} PDF manuals to index.", files.Count);
    if (string.Equals(configuration["PdfExtraction:Provider"], "ContentUnderstanding", StringComparison.OrdinalIgnoreCase))
    {
        var analyzerId = await host.Services.GetRequiredService<AnalyzerConfigurator>().CreateAnalyzerAsync(cancellation.Token);
        logger.LogInformation("Using manual analyzer {AnalyzerId} to generate document abstracts.", analyzerId);
    }
    await host.Services.GetRequiredService<SearchIndexCreator>().CreateVectorIndexAsync(cancellation.Token);

    var indexer = host.Services.GetRequiredService<PdfDocumentIndexer>();
    var totalChunks = 0;
    foreach (var file in files)
    {
        logger.LogInformation("Indexing manual {File}.", file);
        totalChunks += await indexer.IndexPdfAsync(file, cancellation.Token);
    }

    logger.LogInformation("Successfully indexed all {Count} manuals with {Chunks} chunks.", files.Count, totalChunks);
    return 0;
//}
// catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
// {
//     Log.Warning("Manual indexing was cancelled.");
//     return 130;
// }
// catch (Exception exception)
// {
//     Log.Fatal(exception, "Manual indexing failed.");
//     return 1;
// }
// finally
// {
//     await Log.CloseAndFlushAsync();
// }
