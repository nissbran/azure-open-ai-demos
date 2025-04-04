using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
using Microsoft.SemanticKernel.Prompty;

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Demo5.Agents.Starship
{
    public static class StarshipAgent
    {
        public static ChatCompletionAgent CreateAgent(Kernel kernel, IConfiguration configuration)
        {
            var starshipPrompty = File.ReadAllText("./Agents/Starship/starship.prompty");
            var writerTemplateConfig = KernelFunctionPrompty.ToPromptTemplateConfig(starshipPrompty);
            
            var starshipKernel = kernel.Clone();
            
            var apiPlugin = new SwapiShipApiPlugin();
            starshipKernel.Plugins.AddFromObject(apiPlugin);
            
            ChatCompletionAgent vehicleAgent = new(writerTemplateConfig, new LiquidPromptTemplateFactory())
                {
                    Kernel = starshipKernel,
                    Name = "starship_agent",
                    HistoryReducer = new ChatHistoryTruncationReducer(4),
                    Arguments = new KernelArguments(
                        new OpenAIPromptExecutionSettings()
                        {
                            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                        })
                    {
                    }
                };
            return vehicleAgent;
        }
        
        //public static ChatCompletionAgent Create
    }
}
