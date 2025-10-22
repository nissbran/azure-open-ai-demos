using System.Threading.Tasks;
using Demo9.Agents.BoMAgent;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Serilog.Extensions.Logging;

namespace Demo9.Agents.PartSupplierAgent;

public class PartSupplierAgent
{
    private const string SystemMessage = """
                                         Supplies information about where parts can be found at suppliers, including part numbers, cost, and lead time, using parts supplier data. 
                                         Answers user queries based on the provided data and helps users find the information they need efficiently.

                                         Instructions
                                         - Use the part number if provided to refine search results.
                                         - Use data available as source as the primary source for responses. 
                                         - Maintain a professional and helpful tone in all interactions. 
                                         - Do not share or expose the raw data; only provide relevant extracted data. 
                                         - If a part is not found, inform the user politely and suggest possible next steps. 
                                         - Avoid speculation; only use available data. - Ensure all responses are clear, concise, and relevant to the user's request.
                                         
                                         """;

    public static async Task<ChatCompletionAgent> CreateAgent(Kernel kernel, IConfiguration configuration)
    {
        var partSupplierKernel = kernel.Clone();

        if (configuration["AzureAISearch:RebuildIndex"]?.ToLower() == "true")
        {
            Log.Information("Rebuilding the part supplier index as per configuration setting.");
            var indexer = new SearchIndexer(configuration);
            await indexer.CreateIndexAsync();
        }
        else
        {
            Log.Information("Skipping part supplier index rebuild for as per configuration setting.");
        }

        var azureAiSearchPlugin = new PartSupplierSearchPlugin(configuration);
        partSupplierKernel.Plugins.AddFromObject(azureAiSearchPlugin);

        ChatCompletionAgent agent = new ChatCompletionAgent
        {
            Kernel = partSupplierKernel,
            Name = "PartSupplierAgent",
            Description = "This agent has access to suppliers and can provide information about where vehicle parts can be found at suppliers.",
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