using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo2;

public class ChatWithFunctionsService
{
    private readonly OpenAIClient _client;
    private readonly string _model;

    private readonly ChatRequestMessage _systemMessage = new ChatRequestSystemMessage("You are a helpful assistant that helps find information about starships and vehicles in Star Wars.");
    private readonly List<ChatRequestMessage> _memory = new();
    
    private readonly SwapiShipApiFunction _swapiApiFunction = new();
    private readonly SwapiAzureAiSearchFunction _swapiAzureAiSearchFunction;

    public ChatWithFunctionsService(IConfiguration configuration)
    {
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        _model = configuration["AzureOpenAI:ChatModel"];
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _swapiAzureAiSearchFunction = new SwapiAzureAiSearchFunction(configuration);
    }

    public void StartNewSession()
    {
        _memory.Clear();
        _memory.Add(_systemMessage);
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            _memory.Add(new ChatRequestUserMessage(message));
            var options = new ChatCompletionsOptions(_model, _memory);
            options.Functions.Add(_swapiApiFunction.GetFunctionDefinition());
            options.Functions.Add(_swapiAzureAiSearchFunction.GetFunctionDefinition());
            
            var completionsResponse = await _client.GetChatCompletionsAsync(options);

            var functionCallWasMade = await HandleFunctionsResponseMessage(completionsResponse);
            if (functionCallWasMade)
            {
                var optionsFunctionCall = new ChatCompletionsOptions(_model, _memory);
                optionsFunctionCall.Functions.Add(_swapiApiFunction.GetFunctionDefinition());
                optionsFunctionCall.Functions.Add(_swapiAzureAiSearchFunction.GetFunctionDefinition());
                completionsResponse = await _client.GetChatCompletionsAsync(optionsFunctionCall);
            }
            var responseMessage = completionsResponse.Value.Choices[0].Message.Content;
            _memory.Add(new ChatRequestAssistantMessage(responseMessage));
            return responseMessage;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get chat completions");
            return "I'm sorry, I can't do that right now.";
        }
    }

    private async Task<bool> HandleFunctionsResponseMessage(Response<ChatCompletions> completionsResponse)
    {
        foreach (var chatChoice in completionsResponse.Value.Choices)
        {
            if (chatChoice.FinishReason == CompletionsFinishReason.FunctionCall)
            {
                var functionResponse = chatChoice.Message.FunctionCall;
                switch (functionResponse.Name)
                {
                    case SwapiShipApiFunction.FunctionName:
                    {
                        var parameters = JsonSerializer.Deserialize<SwapiShipApiFunction.SwapiShipApiFunctionParameters>(functionResponse.Arguments);
                        var ship = await _swapiApiFunction.CallStarWarsShipApi(parameters);
                        _memory.Add(new ChatRequestFunctionMessage(SwapiShipApiFunction.FunctionName, ship));
                        return true;
                    }
                    case SwapiAzureAiSearchFunction.FunctionName:
                    {
                        var parameters = JsonSerializer.Deserialize<SwapiAzureAiSearchFunction.SwapiAzureAiSearchFunctionParameters>(functionResponse.Arguments);
                        var vehicles = await _swapiAzureAiSearchFunction.GetVehicles(parameters);
                        _memory.Add(new ChatRequestFunctionMessage(SwapiAzureAiSearchFunction.FunctionName, vehicles));
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public async Task<string> TypeMessageWithoutMemory(string message)
    {
        try
        {
            var messages = new List<ChatRequestMessage>
            {
                _systemMessage,
                new ChatRequestUserMessage(message)
            };
            var response = await _client.GetChatCompletionsAsync(new ChatCompletionsOptions(_model, messages));

            var responseMessage = response.Value.Choices.First().Message.Content;
            return responseMessage;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get chat completions");
            return "I'm sorry, I can't do that right now.";
        }
    }

    public async IAsyncEnumerable<string> TypeAndStreamMessageAsync(string message)
    {
        _memory.Add(new ChatRequestUserMessage(message));

        StreamingResponse<StreamingChatCompletionsUpdate> streamingResponse = null;
        var ifError = false;

        try
        {
            streamingResponse = await _client.GetChatCompletionsStreamingAsync(new ChatCompletionsOptions(_model, _memory));
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get chat completions");
            ifError = true;
        }

        if (ifError)
        {
            yield return "I'm sorry, I can't do that right now.";
            yield break;
        }

        var fullMessageBuilder = new StringBuilder();
        await foreach (var update in streamingResponse)
        {
            var content = update.ContentUpdate;
            if (string.IsNullOrEmpty(content))
                continue;
            fullMessageBuilder.Append(content);
            yield return content;
        }

        _memory.Add(new ChatRequestAssistantMessage(fullMessageBuilder.ToString()));
    }
}