using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Chat;
using Serilog;

namespace Demo1;

public class ChatService
{
    private readonly OpenAIClient _client;
    private readonly ChatClient _chatClient;
    private readonly string _model;
    
    private readonly ChatMessage _systemMessage = new SystemChatMessage(
        "You are a helpful assistant. You will talk like a pirate.");
    private readonly List<ChatMessage> _memory = new();
    
    public ChatService(IConfiguration configuration)
    {
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        _model = configuration["AzureOpenAI:ChatModel"];
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _memory.Add(_systemMessage);
        _chatClient = _client.GetChatClient(_model);
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
            _memory.Add(new UserChatMessage(message));
            var response = await _chatClient.CompleteChatAsync(_memory);
            
            var responseMessage = response.Value.Content.First().Text;
            _memory.Add(new AssistantChatMessage(responseMessage));
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
            var response = await _chatClient.CompleteChatAsync(_systemMessage, new UserChatMessage(message));
            
            var responseMessage = response.Value.Content.First().Text;
            
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
        _memory.Add(new UserChatMessage(message));

        AsyncResultCollection<StreamingChatCompletionUpdate> streamingResponse = null;
        var ifError = false;
        
        try
        {
            streamingResponse = _chatClient.CompleteChatStreamingAsync(_memory);
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
            foreach (var messageContentPart in content)
            {
                var text = messageContentPart.Text;
                if (string.IsNullOrEmpty(text))
                    continue;
                fullMessageBuilder.Append(text);
                yield return text;
            }
        }
        
        _memory.Add(new AssistantChatMessage(fullMessageBuilder.ToString()));
    }
}