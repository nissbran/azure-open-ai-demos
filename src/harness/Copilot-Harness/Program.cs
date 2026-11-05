using System;
using System.ClientModel;
using System.Net.Http;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Demo15;
using GitHub.Copilot;
using GitHub.Copilot.Rpc;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Suppress experimental API warnings for Responses API usage.

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

Console.OutputEncoding = System.Text.Encoding.UTF8;

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

var builder = WebApplication.CreateBuilder(args);

var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Endpoint configuration is missing.");
var responsesEndpoint = new Uri($"{endpoint.TrimEnd('/')}/openai/v1/");
var chatClient = new ResponsesClient(
            new ApiKeyCredential(configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("ApiKey configuration is missing.")),
            new ResponsesClientOptions { Endpoint = responsesEndpoint })
        .AsIChatClient(configuration["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing."));

builder.Services.AddChatClient(chatClient);
var copilotClient = new CopilotClient();
var copilotAgent = copilotClient.AsAIAgent(new SessionConfig()
{
    OnPermissionRequest = PermissionHandler.ApproveAll
}, name: "Copilot");
builder.AddAIAgent("Copilot", (provider, s) => copilotAgent);

var webIQTool = new HostedMcpServerTool(
    "WebIQ",
    new Uri("https://api.microsoft.ai/v3/mcp"))
{
    ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire,
    Headers = new Dictionary<string, string>
    {
        ["x-apikey"] = configuration["WebIQ:ApiKey"] ?? throw new InvalidOperationException("WebIQ ApiKey configuration is missing.")
    }
};

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    Name = "Researcher",
    DisableWebSearch = true,
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "execute" },
    ChatOptions = new ChatOptions
    {
        Instructions = "Use the question to provide find information and build a information summary that will be used as context for building a html single index.html page." +
                       "The question is about a location, with pictures and images that can be added to the page and include it in the summary " +
                       "The output should like this: " +
                       "Please build a single index.html page that contains the following information: <information summary>. ",
        
        ModelId = configuration["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing."),
        Tools = [StockTools.CreateGetStockPriceTool(), webIQTool],
        Reasoning = new() { Effort = ReasoningEffort.Medium },
    },
});
builder.AddAIAgent("Researcher", (provider, s) => agent);

builder.AddWorkflow("WebSiteBuilder", (sp, key) => AgentWorkflowBuilder.BuildSequential("WebSiteBuilder", agent, copilotAgent)).AddAsAIAgent(includeWorkflowOutputsInResponse: true);

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

if (builder.Environment.IsDevelopment())
{
    builder.AddDevUI();
}

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (builder.Environment.IsDevelopment())
{
    app.MapDevUI();
}

Console.WriteLine("DevUI is available at: https://localhost:7080/devui");
Console.WriteLine("OpenAI Responses API is available at: https://localhost:7080/v1/responses");
Console.WriteLine("Press Ctrl+C to stop the server.");

app.Run();
