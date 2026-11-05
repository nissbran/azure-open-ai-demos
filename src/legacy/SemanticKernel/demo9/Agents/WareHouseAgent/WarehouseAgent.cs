using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Serilog;
using Serilog.Extensions.Logging;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Demo9.Agents.WareHouseAgent;

public static class WarehouseAgent
{
    private const string SystemMessage = """
        An agent that provides warehouse inventory information and stock quantities for vehicle parts
        
        Instructions
        - Answer questions about warehouse inventory levels, stock quantities, and part availability using data from the warehouse management system.
        - Include warehouse location information when available (warehouse ID, bin locations, etc.).
        - Only use the provided warehouse database as the source for inventory data; do not speculate or use external sources.
        - Respond in a professional and informative manner, suitable for users needing inventory information.
        - Always communicate in English and maintain a helpful, collaborative tone.
        
        Data Search Instructions
        - Always get the current date and time before querying, use this when checking stock levels.
        - Always check if their is a risk that the part is not in stock due to delay in updating the system, especially if the part is very low in stock (less than 5 items)
        - If the part is not in stock or there is a risk it is not in stock, inform the user and suggest alternatives, like order from external suppliers.
        
        """;
        
    public static async Task<ChatCompletionAgent> CreateAgent(Kernel kernel, IConfiguration configuration)
    {
        var warehouseKernel = kernel.Clone();
        
        Log.Information("Creating Warehouse Agent with SQL Server connectivity.");
        
        var warehouseSqlPlugin = new WarehouseSqlPlugin(configuration);
        await warehouseSqlPlugin.SeedData();
        warehouseKernel.Plugins.AddFromObject(warehouseSqlPlugin);
        warehouseKernel.Plugins.AddFromType<CurrentTimePlugin>();
            
        ChatCompletionAgent agent = new ChatCompletionAgent
        {
            Kernel = warehouseKernel,
            Name = "WarehouseAgent",
            Description = "The agent that provides warehouse inventory information and stock quantities for parts. It has no knowledge of vehicle assembly or part suppliers.",
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