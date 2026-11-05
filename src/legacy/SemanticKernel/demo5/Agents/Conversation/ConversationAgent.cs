using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
using Microsoft.SemanticKernel.Prompty;

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Demo5.Agents.Conversation
{
    public static class ConversationAgent
    {
        private const int HistoryLimit = 6;
        private const int ReducerTarget = 4;
        
        public static ChatCompletionAgent CreateAgent(Kernel kernel, IConfiguration configuration)
        {
            var conversationPrompty = File.ReadAllText("./Agents/Conversation/conversation.prompty");
            var writerTemplateConfig = KernelFunctionPrompty.ToPromptTemplateConfig(conversationPrompty);
            
            var historyReducer = new ChatHistorySummarizationReducer(kernel.GetRequiredService<IChatCompletionService>(), ReducerTarget, HistoryLimit);

            ChatCompletionAgent vehicleAgent = new(writerTemplateConfig, new LiquidPromptTemplateFactory())
                {
                    Kernel = kernel,
                    Name = "conversation_agent",
                    HistoryReducer = historyReducer
                };
            return vehicleAgent;
        }
    }
}
