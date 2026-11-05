using Azure;
using Azure.AI.Projects;
using Azure.Identity;
using MaintenanceAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using OpenAI;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Text;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

//Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

Console.OutputEncoding = Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.local.json", true);
builder.Services.AddHttpClient().AddLogging();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateLogger();

var instructions =
    """
        ## Maintenance Agent Instructions

        You are a maintenance agent responsible for assisting with equipment maintenance tasks. 
        You can provide guidance on troubleshooting, repair procedures, and preventive maintenance schedules.
    
        ### Type of Maintenance
        - Troubleshooting
        - Repair Procedures
        - Preventive Maintenance

        ### Area
        - Microsoft Surface Devices 

        ### Working style
        - Always verify information with available tools and documentation.
        - Use SearchDocumentInfo to find specific information in maintenance manuals.
        - Use GetDocumentSummaries to discover manuals and retrieve their stored summaries.
        - Cite the document title and document ID when using retrieved information.
        - Provide clear, step-by-step instructions.
        - Keep track of maintenance logs and update them as necessary.
        """;
//var endpoint = configuration["Foundry:ProjectEndpoint"] ?? throw new InvalidOperationException("Endpoint configuration is missing.");
var endpoint = configuration["FOUNDRY:PROJECT_ENDPOINT"] ?? throw new InvalidOperationException("Endpoint configuration is missing.");
var deploymentName = configuration["FOUNDRY:MODEL"] ?? "gpt-5.4-mini";
var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());
var chatClient = projectClient.GetProjectOpenAIClient().GetResponsesClient().AsIChatClient(deploymentName);

builder.Services.AddChatClient(chatClient);
builder.Services.AddSingleton<DocumentTools>();
builder.Services.AddSingleton<WebIQMcpTool>();

builder.AddDevUI();

builder.AddAIAgent("maintenance-agent", (provider, key) =>
    new ChatClientAgent(
        provider.GetRequiredService<IChatClient>(),
        name: "maintenance-agent",
        instructions: instructions,
        tools: [.. provider.GetRequiredService<DocumentTools>().AsAITools()]));
builder.AddAIAgent("maintenance-harness", (provider, key) =>
{
    return provider.GetRequiredService<IChatClient>().AsHarnessAgent(new HarnessAgentOptions
    {
        Name = key,
        DisableWebSearch = true,
        ChatOptions = new ChatOptions
        {
            Instructions = instructions,
            Tools = [.. provider.GetRequiredService<DocumentTools>().AsAITools(), provider.GetRequiredService<WebIQMcpTool>()],
            Reasoning = new() { Effort = ReasoningEffort.Medium },
        },
    });
});

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

// Map OpenAI API endpoints — DevUI aggregator routes requests here
app.MapOpenAIResponses();
app.MapOpenAIConversations();

app.UseCors();
app.MapDevUI();

await app.RunAsync();
