using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.OpenAI.Assistants;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Demo3;

public class ChatWithAssistantsService
{
    private readonly AssistantsClient _assistantsClient;
    
    private readonly SwapiShipApiFunction _swapiApiFunction = new();
    private readonly SwapiAzureAiSearchFunction _swapiAzureAiSearchFunction;
    
    private readonly Assistant _mathAssistant;
    private readonly Assistant _startWarsAssistant;

    private AssistantThread _assistantThread;
    private bool _useMathAssistant;

    public ChatWithAssistantsService(IConfiguration configuration, bool createAssistants = false)
    {
        var apiKey = configuration["AzureOpenAI:ApiKey"];
        var endpoint = configuration["AzureOpenAI:Endpoint"];
        var model = configuration["AzureOpenAI:ChatModel"];
        _assistantsClient = new AssistantsClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
        _swapiAzureAiSearchFunction = new SwapiAzureAiSearchFunction(configuration);

        if (createAssistants)
        {
            _mathAssistant = _assistantsClient.CreateAssistant(new AssistantCreationOptions(model)
            {
                Name = "Math Assistant",
                Description = "A simple assistant that helps with math problems.",
                Instructions = "A simple assistant that helps with math problems.",
                Tools =
                {
                    new CodeInterpreterToolDefinition()
                }
            });

            _startWarsAssistant = _assistantsClient.CreateAssistant(new AssistantCreationOptions(model)
            {
                Name = "Star Wars Assistant",
                Instructions = "You are a helpful assistant that helps find information about starships and vehicles in Star Wars.",
                Description = "Star Wars Assistant that helps find information about starships and vehicles in Star Wars.",
                Tools =
                {
                    _swapiApiFunction.GetFunctionDefinition(),
                    _swapiAzureAiSearchFunction.GetFunctionDefinition()
                }
            });
        }
        else
        {
            var assistants = _assistantsClient.GetAssistants();

            foreach (var assistant in assistants.Value)
            {
                if (assistant.Name == "Math Assistant")
                {
                    _mathAssistant = assistant;
                }
                else if (assistant.Name == "Star Wars Assistant")
                {
                    _startWarsAssistant = assistant;
                }
            }
        }
    }

    public async Task StartNewAssistantSession()
    {
        Log.Verbose("Starting new assistant thread session");
        _useMathAssistant = false;
        _assistantThread = await _assistantsClient.CreateThreadAsync();
        Log.Verbose("Thread started with id {ThreadId}", _assistantThread.Id);
    }
    
    public void SwitchToMathAssistant()
    {
        Log.Verbose("Switching to math assistant");
        _useMathAssistant = true;
    }

    public async Task<string> TypeMessageAsync(string message)
    {
        try
        {
            return await ExecuteThreadRun(message);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to execute thread run");
            return "I'm sorry, I can't do that right now.";
        }
    }
    
    private async Task<string> ExecuteThreadRun(string message)
    {
        var messageResponse = await _assistantsClient.CreateMessageAsync(_assistantThread.Id, MessageRole.User, message);
        Log.Verbose("Message sent to assistant thread with id {ThreadId}", _assistantThread.Id);

        var assistant = _useMathAssistant ? _mathAssistant : _startWarsAssistant;
        
        var runResponse = await _assistantsClient.CreateRunAsync(_assistantThread, assistant);
        Log.Verbose("Run created with {Assistant} id {RunId} and status {Status} ", assistant.Name, runResponse.Value.Id, runResponse.Value.Status);
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            runResponse = await _assistantsClient.GetRunAsync(_assistantThread.Id, runResponse.Value.Id);
            Log.Verbose("Current run status {Status} for thread with id {ThreadId}", runResponse.Value.Status, runResponse.Value.ThreadId);

            if (runResponse.Value.Status == RunStatus.RequiresAction
                && runResponse.Value.RequiredAction is SubmitToolOutputsAction submitToolOutputsAction)
            {
                var toolOutputs = new List<ToolOutput>();
                foreach (var toolOutput in submitToolOutputsAction.ToolCalls)
                {
                    toolOutputs.Add(await HandleToolCall(toolOutput));
                }
                runResponse = await _assistantsClient.SubmitToolOutputsToRunAsync(runResponse.Value, toolOutputs);
            }
        }
        while (runResponse.Value.Status == RunStatus.Queued || runResponse.Value.Status == RunStatus.InProgress);
        
        Log.Verbose("Run completed with status {Status} for thread with id {ThreadId}", runResponse.Value.Status, runResponse.Value.ThreadId);
        
        var afterRunMessagesResponse = await _assistantsClient.GetMessagesAsync(_assistantThread.Id);

        var returnMessage = "";
        
        foreach (var threadMessage in afterRunMessagesResponse.Value.Data.OrderBy(tm => tm.CreatedAt))
        {
            foreach (var contentItem in threadMessage.ContentItems)
            {
                switch (contentItem)
                {
                    case MessageTextContent textItem:
                        Log.Verbose("{CreatedAt} - {Role}: {Text}", threadMessage.CreatedAt, threadMessage.Role, textItem.Text);
                        if (threadMessage.Role == MessageRole.Assistant)
                        {
                            returnMessage = textItem.Text;
                        }
                        break;
                    case MessageImageFileContent imageFileItem:
                        Log.Verbose("{CreatedAt} - {Role}: <image from ID:{FileId}>", threadMessage.CreatedAt, threadMessage.Role, imageFileItem.FileId);
                        break;
                }
            }
        }
        return returnMessage;
    }
    
    private async Task<ToolOutput> HandleToolCall(RequiredToolCall toolCall)
    {
        if (toolCall is RequiredFunctionToolCall functionToolCall)
        {
            if (functionToolCall.Name == _swapiApiFunction.GetFunctionName())
            {
                var parameters = JsonSerializer.Deserialize<SwapiShipApiFunction.SwapiShipApiFunctionParameters>(functionToolCall.Arguments);
                var ship = await _swapiApiFunction.GetShipInformation(parameters);
                return new ToolOutput(toolCall, ship);
            }

            if (functionToolCall.Name == _swapiAzureAiSearchFunction.GetFunctionName())
            {
                var parameters = JsonSerializer.Deserialize<SwapiAzureAiSearchFunction.SwapiAzureAiSearchFunctionParameters>(functionToolCall.Arguments);
                var vehicles = await _swapiAzureAiSearchFunction.GetVehicles(parameters);
                return new ToolOutput(toolCall, vehicles);
            }
        }
        return null;
    }
}