using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Spectre.Console;

namespace Demo4;

public class ConsoleChat
{
    private readonly ChatWithSemanticKernelService _chatService;
    private readonly ActivitySource _activitySource;
    private const string BotName = "Star Wars Assistant";

    public ConsoleChat(ChatWithSemanticKernelService chatService, ActivitySource activitySource)
    {
        _chatService = chatService;
        _activitySource = activitySource;
    }

    public async Task<ExitReason> StartChatAsync(CancellationToken cancellationToken = default)
    {
        WriteWelcomeMessage();

        _chatService.StartNewSession();

        // This is a root activity for the chat session
        // It will be used to group all the chat messages
        // It will only be logged to OTEL if the chat session is completed with a /q or /clear command
        using var chatRootActivity = _activitySource.StartActivity("Chat");

        try
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                var message = await AnsiConsole.AskAsync<string>("[bold blue]User:[/] ", cancellationToken);
                switch (message)
                {
                    case "/clear":
                        Log.Verbose("Clearing the session");
                        AnsiConsole.Clear();
                        return ExitReason.ClearRequested;
                    case "/q":
                        AnsiConsole.MarkupLine($"[bold red]{BotName}:[/] Goodbye!");
                        return ExitReason.UserRequested;
                    default:
                        using (var activity = _activitySource.StartActivity("ChatMessage"))
                        {
                            var userMessageEvent = new ActivityEvent("user_message", DateTimeOffset.UtcNow, new ActivityTagsCollection([
                                new KeyValuePair<string, object>("message", message)
                            ]));
                            activity?.AddEvent(userMessageEvent);
                            
                            var response = await _chatService.TypeMessageAsync(message);

                            AnsiConsole.Markup($"[bold red]{BotName}:[/] ");
                            AnsiConsole.WriteLine(string.IsNullOrEmpty(response) ? "I'm sorry, I can't do that right now." : response);

                            var botMessageEvent = new ActivityEvent("bot_response", DateTimeOffset.UtcNow, new ActivityTagsCollection([
                                new KeyValuePair<string, object>("message", response)
                            ]));
                            activity?.AddEvent(botMessageEvent);
                        }
                        break;
                }
            }
        }
        catch (TaskCanceledException e)
        {
            return ExitReason.UserRequested;
        }
        
        return ExitReason.UserRequested;
    }

    private void WriteWelcomeMessage()
    {
        AnsiConsole.MarkupLine("[bold green]Welcome to the chat![/]");
        AnsiConsole.MarkupLine("[bold green]The star wars assistant is here to help you![/]");
        AnsiConsole.MarkupLine("[bold green] - Use /clear to clear the session[/]");
        AnsiConsole.MarkupLine("[bold green] - Use /print to print the chat history[/]");
        AnsiConsole.MarkupLine("[bold green] - Use /q to exit[/]");
    }
}

public enum ExitReason
{
    UserRequested,
    ClearRequested,
    Error
}