using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Demo5.Agents.Conversation;
using Demo5.Agents.Starship;
using Demo5.Agents.Vehicle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Serilog;
using Serilog.Extensions.Logging;

namespace Demo5;

public class ChatWithAgentsService
{
#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    private readonly AgentGroupChat _chat;
    private readonly IChatHistoryReducer _chatHistoryReducer;

    private const int ReducerTarget = 2;
    private const int HistoryLimit = 4;

    public ChatWithAgentsService(IConfiguration configuration)
    {
        var model = configuration["AzureOpenAI:ChatModel"] ?? throw new ArgumentNullException(nameof(configuration), "ChatModel configuration is missing.");
        var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "ApiKey configuration is missing.");
        var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException(nameof(configuration), "Endpoint configuration is missing.");
        
        var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, endpoint, apiKey);// add logging
        
        var kernel = builder.Build();

        _chatHistoryReducer = new ChatHistorySummarizationReducer(kernel.GetRequiredService<IChatCompletionService>(), ReducerTarget, HistoryLimit);
        
        var vehicleAgent = VehicleAgent.CreateAgent(kernel, configuration);
        var starshipAgent = StarshipAgent.CreateAgent(kernel, configuration);
        var conversationAgent = ConversationAgent.CreateAgent(kernel, configuration);
        
        var selectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
               Examine the provided RESPONSE and choose the next participant.
               State only the name of the chosen participant without explanation.
               Never choose the participant named in the RESPONSE.

               Determine which participant takes the next turn in a conversation based on question.

               Choose only from these participants:
               - {{{conversationAgent.Name}}}
               - {{{vehicleAgent.Name}}}
               - {{{starshipAgent.Name}}}

               Always follow these rules when choosing the next participant:
               - If RESPONSE is user input, it is {{{conversationAgent.Name}}}'s turn.
               - If RESPONSE is about a star wars vehicle, it is {{{vehicleAgent.Name}}}'s turn.
               - If RESPONSE is about a star wars starship, it is {{{starshipAgent.Name}}}}'s turn.

               RESPONSE:
               {{$history}}
               """,
            safeParameterNames: "history");

        var selectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
        {
            // Always start with the writer agent.
            InitialAgent = conversationAgent,
            // Parse the function response.
            //ResultParser = (result) => result.GetValue<string>() ?? WriterName,
            // The prompt variable name for the history argument.
            HistoryVariableName = "history",
            UseInitialAgentAsFallback = true,
            // Save tokens by not including the entire history in the prompt
            HistoryReducer = _chatHistoryReducer,
        };

        _chat = new AgentGroupChat(conversationAgent, vehicleAgent, starshipAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings()
            {
                SelectionStrategy = selectionStrategy,
                TerminationStrategy = { MaximumIterations = 1 }
            }
        };
    }

    public void StartNewSession()
    {
        Log.Verbose("Starting new session");
        _chat.ResetAsync().Wait();
    }
    
    public async Task PrintTheChatAsync()
    {
        Log.Information("Starting to print the chat history");

        var history = await _chat.GetChatMessagesAsync().ToListAsync();

        history.Reverse();

        foreach (var message in history)
        {
            Log.Information("Role: {Role}, Message: {Message}", message.Role, message.Content);
        }
    }

    public async Task<string> TypeMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringBuilder();
           
            _chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, message));

            await foreach (var messageContent in _chat.InvokeAsync(cancellationToken))
            {
                content.Append(messageContent);
            }

            return content.ToString();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to execute ");
            return "I'm sorry, I can't do that right now.";
        }
    }
}