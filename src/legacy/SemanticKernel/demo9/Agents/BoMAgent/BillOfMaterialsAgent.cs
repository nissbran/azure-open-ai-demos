using System.Threading.Tasks;
using Demo9.Indexers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Serilog.Extensions.Logging;

#pragma warning disable SKEXP0040 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Demo9.Agents.BoMAgent;

public static class BoMAgent
{
    private const string SystemMessage = """
        An agent that answers questions about the bill of material for trucks
        
        Instructions
        - Answer questions about the bill of material (BoM) for trucks, referencing data from the supplied search source. 
        - Provide clear, accurate, and concise responses about what parts are needed to create specific truck models. 
        - Only use the provided source for BoM data; do not speculate or use external sources. 
        - Respond in a professional and informative manner, suitable for users seeking technical details about truck assembly.  
        - Do not provide information unrelated to truck BoM or parts lists. 
        - Always communicate in English and maintain a helpful, collaborative tone.
        
        """;
        
    public static async Task<ChatCompletionAgent> CreateAgent(Kernel kernel, IConfiguration configuration)
    {
        var bomKernel = kernel.Clone();

        if (configuration["AzureAISearch:RebuildIndex"]?.ToLower() == "true")
        {
            Log.Information("Rebuilding the Bill of Materials index as per configuration setting.");
            var indexer = new SearchIndexer(configuration);
            await indexer.CreateIndexAsync();
        }
        else
        {
            Log.Information("Skipping index rebuild for Bill of Materials as per configuration setting.");
        }
        
        var azureAiSearchPlugin = new BillOfMaterialsSearchPlugin(configuration);
        bomKernel.Plugins.AddFromObject(azureAiSearchPlugin);
            
        ChatCompletionAgent agent = new ChatCompletionAgent
        {
            Kernel = bomKernel,
            Name = "BillOfMaterialsAgent",
            Description = "The agent that answers questions about the bill of material for vehicles. It has extensive knowledge want parts are needed to build different vehicle models.",
            Instructions = SystemMessage,
            LoggerFactory = new SerilogLoggerFactory(Log.Logger),
            Arguments = new KernelArguments(
                new OpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
        };
        return agent;
    }
}