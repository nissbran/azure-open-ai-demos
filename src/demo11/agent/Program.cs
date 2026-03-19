using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using Demo11;

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
builder.Services.AddAGUI();
builder.Services.AddSingleton<SharedStateAgent>();

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddChatClient(provider => new AzureOpenAIClient(
                new Uri(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Endpoint configuration is missing.")),
                new AzureKeyCredential(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("ApiKey configuration is missing.")))
            .GetChatClient(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing."))
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry(sourceName: "ChatClient", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the chat client level
            .Build());
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

WebApplication app = builder.Build();

app.UseMiddleware<SseNullSanitizingMiddleware>();

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

// Enable CORS
app.UseCors();

// Map the AG-UI agent endpoint
app.MapAGUI("/agent", app.Services.GetRequiredService<SharedStateAgent>());

await app.RunAsync();
