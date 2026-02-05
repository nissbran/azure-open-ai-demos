using System;
using Azure;
using Azure.AI.OpenAI;
using Demo12;
using Microsoft.Agents.AI.Hosting.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

Console.OutputEncoding = System.Text.Encoding.UTF8;

var appBuilder = FunctionsApplication
    .CreateBuilder(args)
    .ConfigureFunctionsWebApplication()
    .ConfigureTelemetry()
    .ConfigureDurableAgents(options =>
    {
        options.AddAIAgentFactory("AlarmAnalyticsAgent", Agents.CreateAlarmAnalyticsAgent, agentOptions =>
        {
            agentOptions.HttpTrigger.IsEnabled = true;
            agentOptions.McpToolTrigger.IsEnabled = false;
        });
        options.AddAIAgentFactory("SupplierAgent", Agents.CreateSupplierAgent, agentOptions =>
        {
            agentOptions.HttpTrigger.IsEnabled = true;
            agentOptions.McpToolTrigger.IsEnabled = true;
        });
        options.AddAIAgentFactory("CasePublishAgent", Agents.CreateCasePublisherAgent, agentOptions =>
        {
            agentOptions.HttpTrigger.IsEnabled = true;
            agentOptions.McpToolTrigger.IsEnabled = false;
        });
    });
appBuilder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>();

// Services
appBuilder.Services.AddSingleton<IChatClient>(provider => new AzureOpenAIClient(
        new Uri(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Endpoint configuration is missing.")),
        new AzureKeyCredential(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("ApiKey configuration is missing.")))
    .GetChatClient(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing."))
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: "ChatClient", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the chat client level
    .Build());
appBuilder.Services.AddSingleton<Tools.PartSupplierSearch>();

using IHost app = appBuilder.Build();

app.Run();