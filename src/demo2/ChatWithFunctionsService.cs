using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Serilog;

namespace Demo2;

public class ChatWithFunctionsService
{
    private readonly OpenAIClient _client;
    private readonly ChatClient _chatClient;
    private readonly string _model;

    private readonly ChatMessage _systemMessage = new SystemChatMessage(
        "You are a helpful assistant that helps find information about starships and vehicles in Star Wars.");

    private readonly List<ChatMessage> _memory = new();

    private readonly SwapiShipApiFunction _swapiApiFunction = new();
    private readonly VehicleSearchFunction _vehicleSearchFunction;

    public ChatWithFunctionsService(IConfiguration configuration)
    {
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        _model = configuration["AzureOpenAI:ChatModel"];
        _client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _vehicleSearchFunction = new VehicleSearchFunction(configuration);
        _chatClient = _client.GetChatClient(_model);
    }

    public void StartNewSession()
    {
        Log.Verbose("Starting new session");
        _memory.Clear();
        _memory.Add(_systemMessage);
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            bool requiresAction;
            string response = string.Empty;

            do
            {
                requiresAction = false;

                Log.Verbose("Message sent to completions api");

                _memory.Add(new UserChatMessage(message));
                var options = new ChatCompletionOptions();
                options.Tools.Add(_swapiApiFunction.GetToolDefinition());
                options.Tools.Add(_vehicleSearchFunction.GetToolDefinition());

                var chatCompletionResult = await _chatClient.CompleteChatAsync(_memory, options);

                switch (chatCompletionResult.Value.FinishReason)
                {
                    case ChatFinishReason.Stop:
                    {
                        _memory.Add(new AssistantChatMessage(chatCompletionResult));
                        response = chatCompletionResult.Value.Content.First().Text;
                        break;
                    }

                    case ChatFinishReason.ToolCalls:
                    {
                        Log.Verbose("Function call was made, calling completions api again");
                        _memory.Add(new AssistantChatMessage(chatCompletionResult));

                        foreach (var toolCall in chatCompletionResult.Value.ToolCalls)
                        {
                            switch (toolCall.FunctionName)
                            {
                                case SwapiShipApiFunction.FunctionName:
                                {
                                    Log.Verbose("Calling Star Wars ship api function with parameters: {Arguments}", toolCall.FunctionArguments);

                                    var parameters = JsonSerializer.Deserialize<SwapiShipApiFunction.SwapiShipApiFunctionParameters>(toolCall.FunctionArguments);
                                    var ship = await _swapiApiFunction.CallStarWarsShipApi(parameters);
                                    _memory.Add(new ToolChatMessage(toolCall.Id, ship));

                                    break;
                                }
                                case VehicleSearchFunction.FunctionName:
                                {
                                    Log.Verbose("Calling Star Wars Azure AI search function with parameters: {Arguments}", toolCall.FunctionArguments);

                                    var parameters = JsonSerializer.Deserialize<VehicleSearchFunction.SwapiAzureAiSearchFunctionParameters>(toolCall.FunctionArguments);
                                    var vehicles = await _vehicleSearchFunction.GetVehicles(parameters);
                                    _memory.Add(new ToolChatMessage(toolCall.Id, vehicles));

                                    break;
                                }
                                default:
                                    throw new NotImplementedException("Unknown function call");
                            }
                        }

                        Log.Verbose("Function call was made, calling completions api again");
                        requiresAction = true;
                        break;
                    }

                    case ChatFinishReason.Length:
                        throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                    case ChatFinishReason.ContentFilter:
                        throw new NotImplementedException("Omitted content due to a content filter flag.");

                    case ChatFinishReason.FunctionCall:
                        throw new NotImplementedException("Deprecated in favor of tool calls.");

                    default:
                        throw new NotImplementedException(chatCompletionResult.Value.FinishReason.ToString());
                }
            } while (requiresAction);

            return response;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get chat completions");
            return "I'm sorry, I can't do that right now.";
        }
    }
}