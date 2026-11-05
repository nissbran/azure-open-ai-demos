using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Demo9.Agents.BoMAgent;
using Demo9.Agents.PartSupplierAgent;
using Demo9.Agents.VehicleProductionAgent;
using Demo9.Agents.WareHouseAgent;
using Demo9.Orchestration;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Magentic;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Serilog.Extensions.Logging;

namespace Demo9;

public class ChatWithAgentsService
{
#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    private readonly ChatHistory _chat = new();
    private readonly GroupChatOrchestration _orchestration;
    private readonly ContextGroupChatOrchestration _contextGroupChatOrchestration;
    private readonly ChatCompletionAgent _singleAgent;
    //private readonly MagenticOrchestration _orchestration;

    public ChatWithAgentsService(IConfiguration configuration)
    {
        var model = configuration["AzureOpenAI:ChatModel"] ?? throw new ArgumentNullException(nameof(configuration), "ChatModel configuration is missing.");
        var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "ApiKey configuration is missing.");
        var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException(nameof(configuration), "Endpoint configuration is missing.");
        
        var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(model, endpoint, apiKey);// add logging
        
        var kernel = builder.Build();
        
        var magenticManager = new StandardMagenticManager(
            kernel.GetRequiredService<IChatCompletionService>(),
            new OpenAIPromptExecutionSettings())
        {
            MaximumInvocationCount = 5,
        };
        
        var bomAgent = BoMAgent.CreateAgent(kernel, configuration).Result;
        var partSupplierAgent = PartSupplierAgent.CreateAgent(kernel, configuration).Result;
        var vehicleProductionAgent = VehicleProductionAgent.CreateAgent(kernel, configuration).Result;
        var warehouseAgent = WarehouseAgent.CreateAgent(kernel, configuration).Result;
        
        
        OrchestrationMonitor monitor = new();
        // _orchestration = new MagenticOrchestration(
        //     magenticManager,
        //     bomAgent,
        //     partSupplierAgent)
        // {
        //     ResponseCallback = ChatResponseCallback,
        //     LoggerFactory = new SerilogLoggerFactory(Log.Logger)
        // };
            
        _orchestration =
            new(
                new ChatManager(kernel.GetRequiredService<IChatCompletionService>(), bomAgent, partSupplierAgent, vehicleProductionAgent, warehouseAgent)
                {
                    MaximumInvocationCount = 5
                },
                bomAgent,
                partSupplierAgent,
                vehicleProductionAgent,
                warehouseAgent)
            {
                LoggerFactory = new SerilogLoggerFactory(Log.Logger),
                ResponseCallback = ChatResponseCallback,
            };
        
        _contextGroupChatOrchestration =
            new(
                new ContextChatManager(kernel.GetRequiredService<IChatCompletionService>())
                {
                    MaximumInvocationCount = 5
                },
                bomAgent,
                partSupplierAgent,
                vehicleProductionAgent,
                warehouseAgent)
            {
                LoggerFactory = new SerilogLoggerFactory(Log.Logger),
                ResponseCallback = ChatResponseCallback,
            };
        
        var maintenanceKernel = kernel.Clone();
        
        
        //maintenanceKernel.Plugins.AddFromObject(new BillOfMaterialsSearchPlugin(configuration));
        maintenanceKernel.Plugins.AddFromObject(new PartSupplierSearchPlugin(configuration));
        maintenanceKernel.Plugins.AddFromObject(new VehicleProductionSearchPlugin(configuration));
        //maintenanceKernel.Plugins.AddFromObject(new WarehouseSqlPlugin(configuration));

//         _singleAgent = new ChatCompletionAgent
//         {
//             Kernel = maintenanceKernel,
//             Name = "MaintenanceAgent",
//             Description = "The agent that answers questions about truck maintenance and repair.",
//             Instructions = """
//                            An agent that answers questions about truck maintenance and repair, referencing data from the supplied search sources. 
//
//                            Instructions
//                            - Provide clear, accurate, and concise responses about truck maintenance and repair. 
//                            - Only use the provided source for data; do not speculate or use external sources. 
//                            - Respond in a professional and informative manner, suitable for users seeking technical details about truck maintenance and repair.  
//                            - Do not provide information unrelated to truck maintenance or repair. 
//                            - Always communicate in English and maintain a helpful, collaborative tone.
//
//                            """,
//             LoggerFactory = new SerilogLoggerFactory(Log.Logger),
//             Arguments = new KernelArguments(
//                 new OpenAIPromptExecutionSettings()
//                 {
//                     FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
//                 })
//         };
        _singleAgent = new ChatCompletionAgent
        {
            Kernel = maintenanceKernel,
            Name = "PartSupplierAgent",
            Description = "The agent that answers questions about truck maintenance and repair.",
            Instructions = """
                           Supplies information about vehicle parts, including part numbers, cost, and lead time, using parts supplier data. 
                           Answers user queries based on the provided data and helps users find the information they need efficiently.

                           Instructions
                           - Use the part number if provided to refine search results.
                           - Use data available as source as the primary source for responses. 
                           - Respond accurately and efficiently to queries about specific parts or general inventory. 
                           - Maintain a professional and helpful tone in all interactions. 
                           - Guide users on how to request information if their query is unclear.
                           - Do not share or expose the raw data; only provide relevant extracted data. 
                           - If a part is not found, inform the user politely and suggest possible next steps. 
                           - Avoid speculation; only use available data. - Ensure all responses are clear, concise, and relevant to the user's request.

                           """,
            LoggerFactory = new SerilogLoggerFactory(Log.Logger),
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };
    }

    public void StartNewSession()
    {
        Log.Verbose("Starting new session");
        _chat.Clear();
    }
    
    private ValueTask ChatResponseCallback(ChatMessageContent response)
    {
        Log.Information("Agent {Agent} response: {Response}", response.AuthorName, response);
        return ValueTask.CompletedTask;
    }
    
    public async Task PrintTheChatAsync()
    {
        Log.Information("Starting to print the chat history");
        
        // await foreach(var message in _chat.GetChatMessagesAsync())
        // {
        //     Log.Information("Role: {Role}, Message: {Message}", message.Role, message.Content);
        // }
        //
        // history.Reverse();
        //
        // foreach (var message in history)
        // {
        //     Log.Information("Role: {Role}, Message: {Message}", message.Role, message.Content);
        // }
    }

    public async Task<string> TypeMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // _chat.AddUserMessage("Hi, i need to change the battery on a hauler 500");
            // _chat.AddAssistantMessage("For the Hauler 500, you will need the following battery for replacement:\n\n- Part Name: Battery Model-1\n- Part Number: BAT001\n- Quantity: 1\n\nPlease ensure you use Battery Model-1 (Part Number: BAT001) for the Contoso Hauler 500. Let me know if you need installation instructions or additional parts");
            //
            // await foreach (var content in _singleAgent.InvokeAsync(_chat, cancellationToken: cancellationToken))
            // {
            //     var text = content.Message.Content;
            //     
            //     if (text != null) 
            //         _chat.AddAssistantMessage(text);
            //     
            //     Log.Information("Single Agent response: {Response}", text);
            // }
            
            InProcessRuntime runtime = new();
            await runtime.StartAsync(cancellationToken);
            
            _chat.AddUserMessage(message);
            
            var result = await _contextGroupChatOrchestration.InvokeAsync(_chat, runtime, cancellationToken);
            
            var text = await result.GetValueAsync(TimeSpan.FromSeconds(10 * 20), cancellationToken);
            
            await runtime.RunUntilIdleAsync();

            _chat.AddAssistantMessage(text);
            
            //return _chat.LastOrDefault()?.Content;
            //return content.ToString();
            return text;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to execute ");
            return "I'm sorry, I can't do that right now.";
        }
    }
    
    protected sealed class OrchestrationMonitor
    {
        public List<StreamingChatMessageContent> StreamedResponses = [];

        public ChatHistory History { get; } = [];

        public ValueTask ResponseCallback(ChatMessageContent response)
        {
            this.History.Add(response);
            //WriteResponse(response);
            return ValueTask.CompletedTask;
        }

        public ValueTask StreamingResultCallback(StreamingChatMessageContent streamedResponse, bool isFinal)
        {
            this.StreamedResponses.Add(streamedResponse);

            if (isFinal)
            {
                //WriteStreamedResponse(this.StreamedResponses);
                this.StreamedResponses.Clear();
            }

            return ValueTask.CompletedTask;
        }
    }
    
    private sealed class ChatManager : GroupChatManager
    {
        private readonly GroupChatTeam _team = [];

        private readonly IChatCompletionService _chatCompletion;

        public ChatManager(IChatCompletionService chatCompletion,params Agent[] members)
        {
            _chatCompletion = chatCompletion;
            foreach (var member in members)
            {
                _team[member.Name ?? member.Id] = (memberType: member.Name ?? member.Id, description: member.Description);
            }
        }

        private static class Prompts
        {
            public static string Termination(string participants) =>
                $"""
                 You are orchestrator collects and correlate information about truck supply chain, warehouse inventory, and parts management. 
                 If the question is not well formed or just contains conversation, respond with True.
                 You have the following assistants that can help you with your research:
                 {participants}\n
                 You need to determine if you have collected enough information from all the assistants. 
                 If one of the assistants might improve the your answer you need respond False.
                 If you have enough data from the history then provide a concise and accurate answer to the original question, respond with True.
                 """;

            public static string RequestUserInput() =>
                $"""
                 You are orchestrator collects and correlate information about truck parts and warehouse inventory. 
                 If the question is not well formed or just contains conversation, please respond to the conversation in a friendly manner and ask the user to supply more information and then respond with True.
                 You need to determine if you have collected enough information to answer, and if so, you need to respond with False. 
                 Otherwise, respond with True.
                 """;
            
            public static string Selection(string participants) =>
                $"""
                 You are orchestrator collects and correlate information about truck supply chain, warehouse inventory, and parts management. 
                 You need to select the next agent you want to support you in your research. 
                 Here are the names and descriptions of the participants: 
                 {participants}\n
                 You need to determine if you have collected enough information from all the participants. 
                 If one of the assistants might improve the your answer you need to select that participant.
                 Please respond with only the name of the participant you would like to select.
                 """;

            public static string Filter() =>
                $"""
                 You are orchestrator collects and correlate information about truck aftermarket, maintenance, supply chain, and warehouse inventory. 
                 If the original question is not well formed or just contains conversation, please respond to the conversation in a friendly manner and ask the user to supply more information.
                 If you have enough data from the history then provide a concise and accurate answer to the original question. 
                 Please summarize the all the data and provide a closing statement.
                 """;
        }
        
        
        public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default)
        {
            
            return GetResponseAsync<string>(history, Prompts.Filter(), cancellationToken);
        }

        /// <inheritdoc/>
        public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
        {
            var response= GetResponseAsync<string>(history, Prompts.Selection(team.FormatList()), cancellationToken);

            if (history.Any(content => content.Role == AuthorRole.Assistant))
            {
                var lastAssistantMessage = history.Last(content => content.Role == AuthorRole.Assistant);
                var contextMessage = "Context from last agent, please use this to inform your decision: " + lastAssistantMessage.Content;
            
                history.AddUserMessage(contextMessage);
            }

            return response;
        }

        /// <inheritdoc/>
        public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "The AI group chat manager does not request user input." });

        /// <inheritdoc/>
        public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
        {
            GroupChatManagerResult<bool> result = await base.ShouldTerminate(history, cancellationToken);
            if (!result.Value)
            {
                result = await this.GetResponseAsync<bool>(history, Prompts.Termination(_team.FormatList()), cancellationToken);
            }
            return result;
        }
        
        private async ValueTask<GroupChatManagerResult<TValue>> GetResponseAsync<TValue>(ChatHistory history, string prompt, CancellationToken cancellationToken = default)
        {
            OpenAIPromptExecutionSettings executionSettings = new() { ResponseFormat = typeof(GroupChatManagerResult<TValue>) };
            ChatHistory request = [.. history, new ChatMessageContent(AuthorRole.System, prompt)];
            ChatMessageContent response = await _chatCompletion.GetChatMessageContentAsync(request, executionSettings, cancellationToken: cancellationToken);
            string responseText = response.ToString();
            return
                JsonSerializer.Deserialize<GroupChatManagerResult<TValue>>(responseText) ??
                throw new InvalidOperationException($"Failed to parse response: {responseText}");
        }
    }
    
    private sealed class ContextChatManager : ContextGroupChatManager
    {
        private readonly IChatCompletionService _chatCompletion;

        public ContextChatManager(IChatCompletionService chatCompletion)
        {
            _chatCompletion = chatCompletion;
        }

        private static class Prompts
        {
            private const string Context = "truck aftermarket, maintenance, supply chain, and warehouse inventory. ";
            
            public static string Termination(string participants) =>
                $"""
                 You are orchestrator collects and correlate information about {Context}
                 If the question is not well formed or just contains conversation, respond with True.
                 You have the following assistants that can help you with your research:
                 {participants}\n
                 You need to determine if you have collected enough information from all the assistants. 
                 It is IMPORTANT that if one of the assistants can improve the context and answer you need respond False.
                 If you have enough data from the history then provide a concise and accurate answer to the original question, respond with True.
                 """;

            public static string PrepareContextForNextAgent(string agentName, string agentDescription) =>
                $"""
                 You are orchestrator collects and correlate information about {Context}
                 You need to prepare the current history for the next agent.
                 The agent you are preparing the context for is called {agentName}.
                 The description of the agent is as follows: {agentDescription}\n
                 You need to provide context that will help the agent answer the original question.
                 """;
            
            public static string Selection(string participants) =>
                $"""
                 You are orchestrator collects and correlate information about {Context}
                 You need to select the next agent you want to support you in your research. 
                 Here are the names and descriptions of the participants: 
                 {participants}\n
                 You need to determine if you have collected enough information from all the participants. 
                 It is IMPORTANT that if one of the assistants can improve the context and answer you need select that participant.
                 Please respond with only the name of the participant you would like to select.
                 """;

            public static string Filter() =>
                $"""
                 You are orchestrator collects and correlate information about {Context}
                 If the original question is not well formed or just contains conversation, please respond to the conversation in a friendly manner and ask the user to supply more information.
                 If you have enough data from the history then provide a concise and accurate answer to the original question. 
                 Please summarize the all the data and provide a closing statement.
                 """;
        }
        
        /// <inheritdoc/>
        public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default)
        {
            return GetResponseAsync<string>(history, Prompts.Filter(), cancellationToken);
        }

        /// <inheritdoc/>
        public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
        {
            return GetResponseAsync<string>(history, Prompts.Selection(team.FormatList()), cancellationToken);
        }

        /// <inheritdoc/>
        public override ValueTask<GroupChatManagerResult<string>> PrepareContextForNextAgent(ChatHistory history, string nextAgent, string nextAgentDescription, CancellationToken cancellationToken = default)
        {
            return GetResponseAsync<string>(history, Prompts.PrepareContextForNextAgent(nextAgent, nextAgentDescription), cancellationToken);
        }

        /// <inheritdoc/>
        public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
        {
            GroupChatManagerResult<bool> result = await base.ShouldTerminate(history, team, cancellationToken);
            if (!result.Value)
            {
                result = await GetResponseAsync<bool>(history, Prompts.Termination(team.FormatList()), cancellationToken);
            }
            return result;
        }
        
        private async ValueTask<GroupChatManagerResult<TValue>> GetResponseAsync<TValue>(ChatHistory history, string prompt, CancellationToken cancellationToken = default)
        {
            OpenAIPromptExecutionSettings executionSettings = new() { ResponseFormat = typeof(GroupChatManagerResult<TValue>) };
            ChatHistory request = [.. history, new ChatMessageContent(AuthorRole.System, prompt)];
            ChatMessageContent response = await _chatCompletion.GetChatMessageContentAsync(request, executionSettings, cancellationToken: cancellationToken);
            string responseText = response.ToString();
            
            Log.Verbose("Response Text: {ResponseText}", responseText);
            
            return
                JsonSerializer.Deserialize<GroupChatManagerResult<TValue>>(responseText) ??
                throw new InvalidOperationException($"Failed to parse response: {responseText}");
        }
    }
    
    private sealed class HumanInTheLoopChatManager(string authorName, string criticName) : RoundRobinGroupChatManager
    {
        public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default)
        {
            ChatMessageContent finalResult = history.Last(message => message.AuthorName == authorName);
            return ValueTask.FromResult(new GroupChatManagerResult<string>($"{finalResult}") { Reason = "The approved copy." });
        }

        /// <inheritdoc/>
        public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
        {
            // Has the maximum invocation count been reached?
            GroupChatManagerResult<bool> result = await base.ShouldTerminate(history, cancellationToken);
            if (!result.Value)
            {
                // If not, check if the reviewer has approved the copy.
                ChatMessageContent? lastMessage = history.LastOrDefault();
                if (lastMessage is not null && lastMessage.Role == AuthorRole.User && $"{lastMessage}".Contains("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    // If the reviewer approves, we terminate the chat.
                    result = new GroupChatManagerResult<bool>(true) { Reason = "The user is satisfied with the copy." };
                }
            }
            return result;
        }

        public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default)
        {
            ChatMessageContent? lastMessage = history.LastOrDefault();

            if (lastMessage is null)
            {
                return ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "No agents have spoken yet." });
            }

            if (lastMessage is not null && lastMessage.AuthorName == criticName && $"{lastMessage}".Contains("I Approve", StringComparison.OrdinalIgnoreCase))
            {
                return ValueTask.FromResult(new GroupChatManagerResult<bool>(true) { Reason = "User input is needed after the reviewer's message." });
            }

            return ValueTask.FromResult(new GroupChatManagerResult<bool>(false) { Reason = "User input is not needed until the reviewer's message." });
        }
    }
}