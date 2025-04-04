using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Demo3;

public class ChatWithFunctionsService
{
    private readonly IChatClient _chatClient;
    private readonly string _model;

    private readonly List<ChatMessage> _memory = new();

    private readonly SwapiShipApiFunction _swapiApiFunction = new();
    private readonly VehicleSearchFunction _vehicleSearchFunction;

    public ChatWithFunctionsService([FromKeyedServices("StarWars")] IChatClient chatClient, IConfiguration configuration)
    {
        _chatClient = chatClient;
        _vehicleSearchFunction = new VehicleSearchFunction(configuration);
    }

    public void StartNewSession()
    {
        Log.Verbose("Starting new session");
        _memory.Clear();
        _memory.Add(new ChatMessage(ChatRole.System,
            "You are a helpful assistant that helps find information about starships and vehicles in Star Wars."));
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            _memory.Add(new ChatMessage(ChatRole.User, message));

            var options = new ChatOptions
            {
                Tools = new List<AITool>
                {
                    _swapiApiFunction.GetFunctionDefinition(),
                    _vehicleSearchFunction.GetFunctionDefinition()
                }
            };

            var chatCompletionResult = await _chatClient.GetResponseAsync(_memory, options);
            
            _memory.AddRange(chatCompletionResult.Messages);

            return chatCompletionResult.Text;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get chat completions");
            return "I'm sorry, I can't do that right now.";
        }
    }
}