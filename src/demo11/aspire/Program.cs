using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", true);

var server = builder.AddProject<Projects.Demo11_AgentFramework_BasicChat>("Server");
    //.WithExternalHttpEndpoints();

var frontend = builder.AddJavaScriptApp("frontend", "../frontend")
    .WaitFor(server)
    .WithReference(server)
    .WithHttpEndpoint(port: 3000, env: "PORT");

builder.Build().Run();