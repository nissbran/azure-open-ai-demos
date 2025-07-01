using System;
using Demo8;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;


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

var agentId = configuration["AgentService:AgentId"];
var chatService = new ChatWithAgentService(configuration, agentId);

string botName = "Star Wars Assistant";

// Run chat
WriteWelcomeMessage();

await chatService.StartNewSessionAsync();

var useStreaming = true;

while (true)
{
    var message = AnsiConsole.Ask<string>("[bold blue]User:[/] ");
    switch (message)
    {
        case "/clear":
            Log.Verbose("Clearing the session");
            AnsiConsole.Clear();
            await chatService.StartNewSessionAsync();
            botName = "Star Wars Assistant";
            WriteWelcomeMessage();
            break;
        case "/q":
            AnsiConsole.MarkupLine($"[bold green]{botName}:[/] Goodbye!");
            return;
        default:
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
                AnsiConsole.MarkupLine($"[bold red]{botName}:[/] " + response); 
                AnsiConsole.WriteLine(string.IsNullOrEmpty(response) ? "I'm sorry, I can't do that right now." : response);
            }
            break;
    }
}

void WriteWelcomeMessage()
{
    AnsiConsole.MarkupLine("[bold green]Welcome to the chat![/]");
    AnsiConsole.MarkupLine("[bold green]The Star Wars assistant is here to help you![/]");
    AnsiConsole.MarkupLine("[bold green] - Use /clear to clear the session[/]");
    AnsiConsole.MarkupLine("[bold green] - Use /q to exit[/]");
}