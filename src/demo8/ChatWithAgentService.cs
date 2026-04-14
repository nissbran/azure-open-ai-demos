using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;
using Serilog;

namespace Demo8;

public class ChatWithAgentService
{
    private readonly AIProjectClient _client;
    
    private readonly string _mcpBaseUrl;
    private readonly string _model;

    private AIAgent _agent;
    private AgentSession _agentSession;
    

    public ChatWithAgentService(IConfiguration configuration)
    {
        var endpoint = configuration["AgentService:Endpoint"] ?? throw new InvalidOperationException("Agent Service Endpoint is not set.");
        _mcpBaseUrl = configuration["AgentService:McpBaseUrl"];
        _model = configuration["AgentService:Model"];
        
        _client = new(new Uri(endpoint), new AzureCliCredential());
    }

    [Experimental("OPENAI001")]
    public async Task CreateAgentAsync()
    {
        ProjectsAgentVersion agentVersion = await _client.AgentAdministrationClient.CreateAgentVersionAsync(
            "CustomerSupportAgent",
            new ProjectsAgentVersionCreationOptions(
                new DeclarativeAgentDefinition(model: _model)
                {
                    Instructions = "You are a helpful assistant that helps find information about starships and vehicles in Star Wars.",
                    Tools =
                    {
                        //new McpTool("Tool", new Uri(_mcpBaseUrl))
                    }
                }));

        _agent = _client.AsAIAgent(agentVersion);
    }

    public async Task StartNewSessionAsync()
    {
        _agentSession = await _agent.CreateSessionAsync();
        Log.Verbose("Starting new session with ID: {ThreadId}", _agentSession);
    }

    public async IAsyncEnumerable<string> TypeAndStreamMessageAsync(string message)
    {
        IAsyncEnumerable<AgentResponseUpdate> streamingResponse = null;
        var ifError = false;

        try
        {
            streamingResponse = _agent.RunStreamingAsync(message, _agentSession);
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
            yield return streamingUpdate.Text;
        }
    }
}
