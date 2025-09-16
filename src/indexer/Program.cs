using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SwapiIndexer;

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.local.json", true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateLogger();

// Initialize the services
var vehicleReader = new SwapiVehicleReader(configuration);
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