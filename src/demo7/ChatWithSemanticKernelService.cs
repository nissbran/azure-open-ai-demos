using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using Serilog;

#pragma warning disable SKEXP0001

namespace Demo7;

public class ChatWithSemanticKernelService
{
    private const string SystemMessage =
        "You are a helpful assistant that helps find information about your personal github account. Please use the tools available to you to answer the questions. If you don't know the answer, please ask the user for more information.";

    private readonly ChatHistory _history = [];
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;

    private readonly OpenAIPromptExecutionSettings _openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    private readonly IChatHistoryReducer _chatHistoryReducer;
    private readonly string _githubPat;

    private const int ReducerTarget = 2;
    private const int HistoryLimit = 4;

    private IMcpClient _mcpClient;

    public ChatWithSemanticKernelService(IConfiguration configuration)
    {
        var model = configuration["AzureOpenAI:ChatModel"] ?? throw new ArgumentNullException(nameof(configuration), "ChatModel configuration is missing.");
        var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "ApiKey configuration is missing.");
        var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException(nameof(configuration), "Endpoint configuration is missing.");

        var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, endpoint, apiKey);

        // builder.Services.AddLogging(configure => configure.AddConsole());
        // builder.Services.AddLogging(configure => configure.SetMinimumLevel(LogLevel.Trace));

        _githubPat = configuration["Github:PersonalAccessToken"] ?? throw new ArgumentNullException(nameof(configuration), "Github:PersonalAccessToken configuration is missing.");

        _kernel = builder.Build();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        _chatHistoryReducer = new ChatHistorySummarizationReducer(_chatCompletionService, ReducerTarget, HistoryLimit);
    }

    public async Task StartNewSessionAsync()
    {
        Log.Verbose("Starting new session");
        _history.Clear();
        _history.AddSystemMessage(SystemMessage);

        if (_mcpClient != null)
        {
            Log.Verbose("Disposing the old mcpClient");
            await _mcpClient.DisposeAsync().ConfigureAwait(false);
        }

        Log.Verbose("Creating new MCP client");
        var dockerPat = $"GITHUB_PERSONAL_ACCESS_TOKEN={_githubPat}".ToString();
        _mcpClient = await McpClientFactory.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = "docker",
                Arguments = new List<string>()
                {
                    "run", "-i", "--rm", "-e", dockerPat, "ghcr.io/github/github-mcp-server:v0.2.1"
                },
            })).ConfigureAwait(false);
        var tools = await _mcpClient.ListToolsAsync().ConfigureAwait(false);

        Log.Verbose("Found {Count} tools", tools.Count);

        foreach (var tool in tools)
        {
            Log.Verbose("Tool: {Name} - {Description}", tool.Name, tool.Description);
        }

        _kernel.Plugins.Clear();
        _kernel.Plugins.AddFromFunctions("Github", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            if (await _history.ReduceInPlaceAsync(_chatHistoryReducer, CancellationToken.None))
            {
                Log.Information("Chat history reduced to {Count} messages with {Summary}",
                    _history.Count,
                    _history.Where(c => c.Metadata != null && c.Metadata.ContainsKey("__summary__")).Select(content => content.Content));
            }

            _history.AddUserMessage(message);

            var response = await _chatCompletionService.GetChatMessageContentAsync(_history, _openAIPromptExecutionSettings, _kernel);

            _history.AddMessage(response.Role, response.Content ?? string.Empty);

            return response.Content;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to execute ");
            return "I'm sorry, I can't do that right now.";
        }
    }
}