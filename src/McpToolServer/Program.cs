using Azure;
using Azure.Search.Documents;
using McpToolServer.Tools;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen);
});
builder.Configuration.AddJsonFile("appsettings.local.json", true);

builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new SearchClient(new Uri(configuration["AzureAISearch:Endpoint"]), "swapi-vehicle-index", new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
});
builder.Services.AddHttpClient("SwapiClient", client =>
{
    var configuration = builder.Configuration;
    client.BaseAddress = new Uri(configuration["Swapi:Url"]);
});

builder.Services.AddMcpServer()
    .WithTools<VehicleSearchTool>()
    .WithTools<ShipTool>();

var app = builder.Build();

app.MapMcp();

app.Run();