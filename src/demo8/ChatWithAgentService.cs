using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo8;

public class ChatWithAgentService
{
    private PersistentAgentsClient _client;
    private PersistentAgentThread _currentThread;
    
    private readonly string _agentServiceEndpoint;
    private readonly string _mcpBaseUrl;
    private readonly string _agentId;
    
    private const string AgentServiceApiVersion = "2025-05-15-preview";

    public ChatWithAgentService(IConfiguration configuration, string agentId)
    {
        _agentServiceEndpoint = configuration["AgentService:Endpoint"];
        _mcpBaseUrl = configuration["AgentService:McpBaseUrl"];
        _client = new(_agentServiceEndpoint, new DefaultAzureCredential());
        _agentId = agentId ?? throw new ArgumentNullException(nameof(agentId), "Agent ID cannot be null.");
    }

    public async Task CreateAgentAsync()
    {
        var credentials = new DefaultAzureCredential();
        
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_agentServiceEndpoint);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await credentials.GetTokenAsync(new TokenRequestContext(["https://ai.azure.com/.default"]))).Token);

        var request = new AssistantRequest
        {
            instructions = "You are a customer support chatbot. Use the tools provided and your knowledge base to best respond to customer queries.",
            tools =
            [
                new Tool
                {
                    type = "mcp",
                    server_label = "McpServer",
                    server_url = _mcpBaseUrl,
                    require_approval = "never"
                }
            ],
            name = "my-assistant-sw-sse",
            model = "gpt-4.1"
        };
        
        await httpClient.PostAsJsonAsync($"assistants?api-version={AgentServiceApiVersion}", request);
    }

    public async Task StartNewSessionAsync()
    {
        _currentThread = await _client.Threads.CreateThreadAsync();
        Log.Verbose("Starting new thread with ID: {ThreadId}", _currentThread.Id);
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            await _client.Messages.CreateMessageAsync(
                _currentThread.Id,
                MessageRole.User,
                message);

            ThreadRun run = await _client.Runs.CreateRunAsync(_currentThread.Id, _agentId);

            do
            {
                await Task.Delay(500);
                run = await _client.Runs.GetRunAsync(_currentThread.Id, run.Id);
                Log.Verbose("Run status: {Status}", run.Status);
            } while (run.Status == RunStatus.Queued || run.Status == RunStatus.InProgress || run.Status == RunStatus.RequiresAction);

            Pageable<PersistentThreadMessage> messages = _client.Messages.GetMessages(
                threadId: _currentThread.Id,
                order: ListSortOrder.Descending);

            foreach (PersistentThreadMessage threadMessage in messages)
            {
                foreach (MessageContent content in threadMessage.ContentItems)
                {
                    if (content is MessageTextContent textItem)
                        return textItem.Text;
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to execute ");
            return "I'm sorry, I can't do that right now.";
        }

        return "No response received.";
    }

    public async IAsyncEnumerable<string> TypeAndStreamMessageAsync(string message)
    {
        await _client.Messages.CreateMessageAsync(
            _currentThread.Id,
            MessageRole.User,
            message);

        AsyncCollectionResult<StreamingUpdate> streamingResponse = null;
        var ifError = false;

        try
        {
            streamingResponse = _client.Runs.CreateRunStreamingAsync(_currentThread.Id, _agentId);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to start streaming run");
            ifError = true;
        }

        if (ifError)
        {
            yield return "I'm sorry, I can't do that right now.";
            yield break;
        }

        await foreach (var streamingUpdate in streamingResponse)
        {
            if (streamingUpdate.UpdateKind == StreamingUpdateReason.RunCreated)
            {
                Log.Verbose("--- Run started! ---");
            }
            else if (streamingUpdate is MessageContentUpdate contentUpdate)
            {
                yield return contentUpdate.Text;
            }
        }
    }
}

public class Tool
{
    public string type { get; set; }
    public string server_label { get; set; }
    public string server_url { get; set; }
    public string require_approval { get; set; }
}

public class AssistantRequest
{
    public string instructions { get; set; }
    public List<Tool> tools { get; set; }
    public string name { get; set; }
    public string model { get; set; }
}