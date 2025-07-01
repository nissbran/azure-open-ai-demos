using System;
using System.ClientModel;
using Azure.AI.OpenAI;
using Demo3;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Spectre.Console;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = Host.CreateApplicationBuilder();

builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile("appsettings.local.json", true);

builder.Services.AddSerilog(configuration =>
    configuration
        .MinimumLevel.Information()
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen));

builder.Services.AddSingleton(
    new AzureOpenAIClient(new Uri(builder.Configuration["AzureOpenAI:Endpoint"]), new ApiKeyCredential(builder.Configuration["AzureOpenAI:ApiKey"])));

builder.Services.AddKeyedChatClient("StarWars", 
        provider => provider.GetRequiredService<AzureOpenAIClient>()
            .GetChatClient(builder.Configuration["AzureOpenAI:ChatModel"])
            .AsIChatClient())
    .UseFunctionInvocation()
    .UseLogging();
builder.Services.AddSingleton<ChatWithFunctionsService>();

// Create chat service
string botName = "Star Wars Assistant";

// Run chat
var app = builder.Build();

WriteWelcomeMessage();

var chatService = app.Services.GetRequiredService<ChatWithFunctionsService>();

chatService.StartNewSession();

while (true)
{
    var message = AnsiConsole.Ask<string>("[bold blue]User:[/] ");
    switch (message)
    {
        case "/clear":
            Log.Verbose("Clearing the session");
            AnsiConsole.Clear();
            chatService.StartNewSession();
            botName = "Star Wars Assistant";
            WriteWelcomeMessage();
            break;
        case "/q":
            AnsiConsole.MarkupLine($"[bold red]{botName}:[/] Goodbye!");
            return;
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