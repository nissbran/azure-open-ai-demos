using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.local.json", true);

var server = builder.AddProject<Projects.Demo11_AgentFramework_BasicChat>("Server");

#pragma warning disable ASPIREJAVASCRIPT001
var frontend = builder.AddNextJsApp("frontend", "../frontend")
    .WaitFor(server)
    .WithReference(server)
    .WithHttpEndpoint(port: 3000, env: "PORT");
#pragma warning restore ASPIREJAVASCRIPT001



builder.Build().Run();