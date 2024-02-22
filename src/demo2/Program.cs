using Demo2;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.local.json", true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateLogger();

// Create chat service
var chatService = new ChatWithFunctionsService(configuration);
const string botName = "Star Wars Assistant";
const bool useStreaming = false;
const bool useMemory = true;

// Run chat
WriteWelcomeMessage();

while (true)
{
    var message = AnsiConsole.Ask<string>("[bold blue]User:[/] ");
    switch (message)
    {
        case "/clear":
            AnsiConsole.Clear();
            chatService.StartNewSession();
            WriteWelcomeMessage();
            break;
        case "/q":
            AnsiConsole.MarkupLine($"[bold red]{botName}:[/] Goodbye!");
            return;
        default:
            if (!useMemory)
            {
                var response = await chatService.TypeMessageWithoutMemory(message);
                AnsiConsole.MarkupLine($"[bold red]{botName}:[/] " + response);
                break;
            }
            
            if (useStreaming)
            {
                AnsiConsole.Markup($"[bold red]{botName}:[/] "); 
                await foreach (var chunk in chatService.TypeAndStreamMessageAsync(message))
                {
                    AnsiConsole.Write(chunk);
                }
                AnsiConsole.WriteLine();
                break;
            }
            else
            {
                var response = await chatService.TypeMessageAsync(message);
                AnsiConsole.Markup($"[bold red]{botName}:[/] ");
                AnsiConsole.WriteLine(string.IsNullOrEmpty(response) ? "I'm sorry, I can't do that right now." : response);
            }
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