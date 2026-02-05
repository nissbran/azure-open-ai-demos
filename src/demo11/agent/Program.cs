using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using System.Security.Cryptography;
using System.Text;

//Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

Console.OutputEncoding = Encoding.UTF8;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.local.json", true);
builder.Services.AddHttpClient().AddLogging();
builder.Services.AddAGUI();

WebApplication app = builder.Build();

var model = builder.Configuration["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing.");
var apiKey = builder.Configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("ApiKey configuration is missing.");
var endpoint = builder.Configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Endpoint configuration is missing.");

// Create the AI agent
ChatClient chatClient = new AzureOpenAIClient(
        new Uri(endpoint),
        new ApiKeyCredential(apiKey))
    .GetChatClient(model);

AIAgent agent = chatClient.AsIChatClient().AsAIAgent(
    name: "AGUIAssistant",
    instructions: "You are a helpful assistant.");

// Map the AG-UI agent endpoint
app.MapAGUI("/", agent);

await app.RunAsync();
