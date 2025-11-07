using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Demo10;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string SourceName = "Demo10";
const string ServiceName = "Demo10";

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.local.json", true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .WriteTo.OpenTelemetry()
    .CreateLogger();

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService(ServiceName);

using var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource(SourceName, "ChatClient") // Our custom activity source
    .AddSource("Microsoft.Agents.AI*") // Agent Framework telemetry
    .AddSource("AlarmAnalyticsAgent", "SupplierAgent", "SummaryAgent", "CasePublishAgent") // Our agents
    .AddHttpClientInstrumentation() // Capture HTTP calls to OpenAI
    .AddOtlpExporter()
    .AddAzureMonitorTraceExporter(options => options.ConnectionString = configuration["ApplicationInsights:ConnectionString"])
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter(SourceName) // Our custom meter
    .AddMeter("Microsoft.Agents.AI*") // Agent Framework metrics
    .AddHttpClientInstrumentation() // HTTP client metrics
    .AddOtlpExporter()
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(loggingBuilder => loggingBuilder
    .SetMinimumLevel(LogLevel.Debug)
    .AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddOtlpExporter();
        if (!string.IsNullOrWhiteSpace(configuration["ApplicationInsights:ConnectionString"]))
        {
            options.AddAzureMonitorLogExporter(exporterOptions => exporterOptions.ConnectionString = configuration["ApplicationInsights:ConnectionString"]);
        }
        options.IncludeScopes = true;
        options.IncludeFormattedMessage = true;
    }));
serviceCollection.AddSingleton<AgentProcess>();
serviceCollection.AddSingleton<IConfiguration>(configuration);
var serviceProvider = serviceCollection.BuildServiceProvider();

var activitySource = new ActivitySource(SourceName);


// Create chat service
var process = serviceProvider.GetRequiredService<AgentProcess>();

await process.ConfigureWorkflowAsync();

var alarmFile = File.ReadAllText("alarm1.json");

// Run chat
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};

Log.Information("Starting the alarm processing...");

using var chatRootActivity = activitySource.StartActivity("AlarmProcessing");
var result = await process.RunAsync(alarmFile, cts.Token);
    
Log.Information("Process completed. Messages in the chain:");

foreach (var message in result)
{
    Log.Information("{Role}: {Message}", message.Role, message.Text);
}

Console.ReadLine();