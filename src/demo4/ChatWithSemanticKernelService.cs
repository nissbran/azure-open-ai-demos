using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;

namespace Demo4;

public class ChatWithSemanticKernelService
{
    private const string SystemMessage = "You are a helpful assistant that helps find information about starships and vehicles in Star Wars.";
    private readonly ChatHistory _history = new();
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly OpenAIPromptExecutionSettings _openAIPromptExecutionSettings = new() 
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    public ChatWithSemanticKernelService(IConfiguration configuration)
    {
        var model = configuration["AzureOpenAI:ChatModel"];
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, endpoint, apiKey);
        
        _kernel = builder.Build();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        _kernel.Plugins.AddFromType<SwapiShipApiPlugin>();
        var swapiAzureAiSearchPlugin = new SwapiAzureAiSearchPlugin(configuration);
        _kernel.Plugins.AddFromObject(swapiAzureAiSearchPlugin);
    }

    public void StartNewSession()
    {
        Log.Verbose("Starting new session");
        _history.Clear();
        _history.AddSystemMessage(SystemMessage);
    }
    
    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
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