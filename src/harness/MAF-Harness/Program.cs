using System;
using System.ClientModel;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Demo15;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // Suppress experimental API warnings for Responses API usage.

// Load .env file if it exists
DotNetEnv.Env.TraversePath().Load();

Console.OutputEncoding = System.Text.Encoding.UTF8;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false)
    .AddJsonFile("appsettings.local.json", true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>()
    .Build();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .ReadFrom.Configuration(configuration)
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen)
    .CreateLogger();

var instructions =
    """
        ## Personal Finance Assistant Instructions
    
        You are a personal finance and investing assistant. When asked about a stock, look up its
        current price with the get_stock_price tool, and use web search for recent news, earnings,
        or analyst commentary.
    
        ### Working style
        - Always verify numbers with a tool rather than relying on memory. Stock prices change.
        - Cite web sources inline when you use them.
        - Keep the user's watchlist in a memory file called `watchlist.md`: read it when reviewing
          the watchlist, and update it whenever the user adds or removes a ticker.
        """;
var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Endpoint configuration is missing.");
var responsesEndpoint = new Uri($"{endpoint.TrimEnd('/')}/openai/v1/");
var chatClient = new ResponsesClient(
            new ApiKeyCredential(configuration["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("ApiKey configuration is missing.")),
            new ResponsesClientOptions { Endpoint = responsesEndpoint })
        .AsIChatClient(configuration["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing."));

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    ChatOptions = new ChatOptions
    {
        Instructions = instructions,
        ModelId = configuration["AzureOpenAI:ChatModel"] ?? throw new InvalidOperationException("ChatModel configuration is missing."),
        Tools = [StockTools.CreateGetStockPriceTool()],
        Reasoning = new() { Effort = ReasoningEffort.Medium },
    },
});

var result = await agent.RunAsync(messages: new List<ChatMessage>
{
    new ChatMessage(ChatRole.User, "Lets test this")
});

Console.WriteLine(result.Text);
