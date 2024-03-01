using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo1;

public class ChatService
{
    private readonly OpenAIClient _client;
    private readonly string _model;
    
    private readonly ChatRequestMessage _systemMessage = new ChatRequestSystemMessage("You are a helpful assistant. You will talk like a pirate.");
    private readonly List<ChatRequestMessage> _memory = new();
    
    public ChatService(IConfiguration configuration)
    {
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        _model = configuration["AzureOpenAI:ChatModel"];
        _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _memory.Add(_systemMessage);
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
            var response = await _client.GetChatCompletionsAsync(new ChatCompletionsOptions(_model, _memory));
            
            var responseMessage = response.Value.Choices.First().Message.Content;
            _memory.Add(new ChatRequestAssistantMessage(responseMessage));
            return responseMessage;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to get chat completions");
            return "I'm sorry, I can't do that right now.";
        }
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