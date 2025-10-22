using McpInstructionsSupportServer.Prompts;
using McpInstructionsSupportServer.Tools;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen);
});
builder.Configuration
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .AddJsonFile("appsettings.local.json", true);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SetupBuildPipelineTool>()
    .WithPrompts<SetupCIPrompts>();

var app = builder.Build();

app.MapMcp();

app.Run();