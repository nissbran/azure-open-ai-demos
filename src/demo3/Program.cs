using Demo3;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile("appsettings.local.json", true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateLogger();

// Create chat service
var chatService = new ChatWithAssistantsService(configuration);
string botName = "Star Wars Assistant";

// Run chat
WriteWelcomeMessage();

await chatService.StartNewAssistantSession();

while (true)
{
    var message = AnsiConsole.Ask<string>("[bold blue]User:[/] ");
    switch (message)
    {
        case "/clear":
            Log.Verbose("Clearing the session");
            AnsiConsole.Clear();
            await chatService.StartNewAssistantSession();
            botName = "Star Wars Assistant";
            WriteWelcomeMessage();
            break;
        case "/q":
            AnsiConsole.MarkupLine($"[bold red]{botName}:[/] Goodbye!");
            return;
        case "/math":
            AnsiConsole.MarkupLine("[bold green]Switching to math assistant[/]");
            botName = "Math Assistant";
            chatService.SwitchToMathAssistant();
            break;
        default:
            var response = await chatService.TypeMessageAsync(message);
            AnsiConsole.Markup($"[bold red]{botName}:[/] ");
            AnsiConsole.WriteLine(string.IsNullOrEmpty(response) ? "I'm sorry, I can't do that right now." : response);
            break;
    }
}

void WriteWelcomeMessage()
{
    AnsiConsole.MarkupLine("[bold green]Welcome to the chat![/]");
    AnsiConsole.MarkupLine("[bold green]The star wars assistant is here to help you![/]");
    AnsiConsole.MarkupLine("[bold green] - Use /clear to clear the session[/]");
    AnsiConsole.MarkupLine("[bold green] - Use /q to exit[/]");
}