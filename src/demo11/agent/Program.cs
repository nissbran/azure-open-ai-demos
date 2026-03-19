using Azure;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
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

builder.Services.AddChatClient(provider => new AzureOpenAIClient(
                new Uri(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Endpoint configuration is missing.")),
                new AzureKeyCredential(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("ApiKey configuration is missing.")))
            .GetChatClient(provider.GetRequiredService<IConfiguration>()["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing."))
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry(sourceName: "ChatClient", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the chat client level
            .Build());

WebApplication app = builder.Build();

app.UseCors();

app.UseMiddleware<SseNullSanitizingMiddleware>();

app.MapAGUI("/agent", app.Services.GetRequiredService<SharedStateAgent>());

await app.RunAsync();
