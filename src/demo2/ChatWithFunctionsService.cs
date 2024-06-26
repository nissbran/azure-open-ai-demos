﻿using System;
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

    private readonly ChatRequestMessage _systemMessage = new ChatRequestSystemMessage(
        "You are a helpful assistant that helps find information about starships and vehicles in Star Wars.");
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
        Log.Verbose("Starting new session");
        _memory.Clear();
        _memory.Add(_systemMessage);
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            Log.Verbose("Message sent to completions api");
            _memory.Add(new ChatRequestUserMessage(message));
            var options = new ChatCompletionsOptions(_model, _memory);
            options.Functions.Add(_swapiApiFunction.GetFunctionDefinition());
            options.Functions.Add(_swapiAzureAiSearchFunction.GetFunctionDefinition());
            
            var completionsResponse = await _client.GetChatCompletionsAsync(options);
            
            var functionCallWasMade = await HandleFunctionsResponseMessage(completionsResponse);
            
            Log.Verbose("Message received from completions api with function call: {FunctionCallWasMade}", functionCallWasMade);
            if (functionCallWasMade)
            {
                Log.Verbose("Function call was made, calling completions api again");
                var optionsFunctionCall = new ChatCompletionsOptions(_model, _memory);
                optionsFunctionCall.Functions.Add(_swapiApiFunction.GetFunctionDefinition());
                optionsFunctionCall.Functions.Add(_swapiAzureAiSearchFunction.GetFunctionDefinition());
                completionsResponse = await _client.GetChatCompletionsAsync(optionsFunctionCall);
                
                // Check if function call was made again if both functions were called
                var functionCallWasMadeAgain = await HandleFunctionsResponseMessage(completionsResponse);
                if (functionCallWasMadeAgain)
                {
                    Log.Verbose("Function call was made, calling completions api again");
                    var optionsFunctionCall2 = new ChatCompletionsOptions(_model, _memory);
                    optionsFunctionCall2.Functions.Add(_swapiApiFunction.GetFunctionDefinition());
                    optionsFunctionCall2.Functions.Add(_swapiAzureAiSearchFunction.GetFunctionDefinition());
                    completionsResponse = await _client.GetChatCompletionsAsync(optionsFunctionCall2);
                }
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
                        Log.Verbose("Calling Star Wars ship api function with parameters: {Arguments}", functionResponse.Arguments);
                        var parameters = JsonSerializer.Deserialize<SwapiShipApiFunction.SwapiShipApiFunctionParameters>(functionResponse.Arguments);
                        var ship = await _swapiApiFunction.CallStarWarsShipApi(parameters);
                        _memory.Add(new ChatRequestFunctionMessage(SwapiShipApiFunction.FunctionName, ship));
                        return true;
                    }
                    case SwapiAzureAiSearchFunction.FunctionName:
                    {
                        Log.Verbose("Calling Star Wars Azure AI search function with parameters: {Arguments}", functionResponse.Arguments);
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
}