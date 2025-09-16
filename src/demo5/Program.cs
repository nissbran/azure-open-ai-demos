using System;
using System.Diagnostics;
using System.Threading;
using Demo5;
using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

Console.OutputEncoding = System.Text.Encoding.UTF8;

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
    .CreateLogger();

// Add telemetry
AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("Demo5");

using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddHttpClientInstrumentation()
    .AddSource("Microsoft.SemanticKernel*", "Demo5")
    .AddOtlpExporter()
    //.AddAzureMonitorTraceExporter(options => options.ConnectionString = configuration["ApplicationInsights:ConnectionString"])
    .Build();

var activitySource = new ActivitySource("Demo5");

// Create chat service
var chatService = new ChatWithAgentsService(configuration);
var consoleChat = new ConsoleChat(chatService, activitySource);

// Run chat
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    cts.Cancel();
    e.Cancel = true;
};

while (await consoleChat.StartChatAsync(cts.Token) == ExitReason.ClearRequested)
{
    // Loop
}