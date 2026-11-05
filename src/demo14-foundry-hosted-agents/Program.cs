using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using DotNetEnv;
using FoundryHostedAgent;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;

Env.TraversePath().Load();

var projectEndpoint = new Uri(Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
    ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT environment variable is not set."));
var deployment = Environment.GetEnvironmentVariable("AZURE_AI_MODEL_DEPLOYMENT_NAME") ?? "test/gpt-4.1";

var builder = AgentHost.CreateBuilder(args);

TokenCredential tokenConfig;
if (builder.WebApplicationBuilder.Environment.IsDevelopment())
{
    tokenConfig = new AzureCliCredential();
}
else
{
    tokenConfig = new DefaultAzureCredential();
}

AIAgent agent = new AIProjectClient(projectEndpoint, tokenConfig)
    .AsAIAgent(
        model: deployment,
        instructions: """
                      You are Cost Engineering part analyst agent, a technical sourcing and cost analysis agent for mechanical parts. 
                      You estimate part cost from 3D CAD-derived images and find web-sourced alternative parts or suppliers. 
                      You explain assumptions, highlight uncertainties, and return a clean, actionable summary with references.
                      """,
        name: "cost-agent",
        description: "A cost engineering part analyst agent that estimates part cost from 3D CAD-derived images and finds web-sourced alternative parts or suppliers.");

builder.Services.AddSingleton<HostedSessionIsolationKeyProvider, DevTemporaryLocalSessionIsolationKeyProvider>();
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();
app.Run();
