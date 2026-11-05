using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Serilog.Extensions.Logging;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Demo9.Agents.VehicleProductionAgent;

public static class VehicleProductionAgent
{
    private const string SystemMessage = """
        An agent that answers questions about vehicle production data, including VIN numbers, build dates, models, and production status
        
        Instructions
        - Answer questions about vehicle production data, referencing data from the supplied search source. 
        - Provide clear, accurate, and concise responses about vehicle manufacturing information including VIN numbers, build dates, production status, plant locations, and vehicle specifications. 
        - Only use the provided source for vehicle production data; do not speculate or use external sources. 
        - Respond in a professional and informative manner, suitable for users seeking technical details about vehicle production and manufacturing. 
        - Do not provide information unrelated to vehicle production or manufacturing data. 
        - Always communicate in English and maintain a helpful, collaborative tone.
        - When asked about specific VINs, provide complete production details including build date, model, status, plant location, engine type, color, and options.
        - When asked about production status, provide current status information and any relevant production details.
        - When asked about vehicle models, provide information about all vehicles of that model including production statistics.
        
        """;
        
    public static async Task<ChatCompletionAgent> CreateAgent(Kernel kernel, IConfiguration configuration)
    {
        var vehicleKernel = kernel.Clone();

        if (configuration["AzureAISearch:RebuildIndex"]?.ToLower() == "true")
        {
            Log.Information("Rebuilding the Vehicle Production index as per configuration setting.");
            var indexer = new VehicleProductionSearchIndexer(configuration);
            await indexer.CreateIndexAsync();
        }
        else
        {
            Log.Information("Skipping index rebuild for Vehicle Production as per configuration setting.");
        }
        
        var azureAiSearchPlugin = new VehicleProductionSearchPlugin(configuration);
        vehicleKernel.Plugins.AddFromObject(azureAiSearchPlugin);
            
        ChatCompletionAgent agent = new ChatCompletionAgent
        {
            Kernel = vehicleKernel,
            Name = "VehicleProductionAgent",
            Description = "The agent that answers questions about vehicle production data including VIN numbers, build dates, models, and production status.",
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