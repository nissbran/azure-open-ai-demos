using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", true);

var indexer = builder.AddProject<Projects.MaintenanceManualsIndexer>("manuals-indexer");

builder.AddProject<Projects.MaintenanceAgent>("maintenance-agent")
    .WaitForCompletion(indexer);
builder.Build().Run();