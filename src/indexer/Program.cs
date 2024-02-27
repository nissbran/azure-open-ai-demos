using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SwapiIndexer;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile("appsettings.local.json", true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateLogger();

// Initialize the services
var vehicleReader = new SwapiVehicleReader();
var vehicleSearchIndexCreator = new SwapiVehicleSearchIndexCreator(configuration);
var vehicleSearchIndexer = new SwapiVehicleSearchIndexer(configuration);
var vehicleVectorizer = new VehicleVectorizer(configuration);

Log.Information("Starting swapi vehicle indexing pipeline with Azure OpenAI");

// Run pipeline
var vehicles = await vehicleReader.GetVehicles();
await vehicleVectorizer.Vectorize(vehicles);
await vehicleSearchIndexCreator.CreateVectorIndexAsync();
await vehicleSearchIndexer.ImportIntoSearch(vehicles);

Log.Information("Finished indexing {Count} vehicles", vehicles.Count);