using System;
using System.Collections.Generic;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace Demo12;

public static class Agents
{
    private const string AnalyticsInstructions = """
                                                 You are an expert in analyzing alarm data from trucks. Your task is to help users understand and interpret alarm analytics data, identify patterns, and provide insights based on the data provided.

                                                 Instructions
                                                 - The alarm will be provided as a JSON.
                                                 - Analyze the alarm data to identify trends, anomalies, and potential issues.
                                                 - Extract the part information from the alarm data.

                                                 """;

    public static AIAgent CreateAlarmAnalyticsAgent(IServiceProvider sp)
    {
        var client = sp.GetRequiredService<IChatClient>();

        return new ChatClientAgent(
                client,
                name: "AlarmAnalyticsAgent",
                description: "Alarm analytics agent",
                instructions: AnalyticsInstructions,
                tools: new List<AITool>())
            .AsBuilder()
            .UseOpenTelemetry("AlarmAnalyticsAgent", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();
    }

    private const string SupplierPartsInstructions = """
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

    public static AIAgent CreateSupplierAgent(IServiceProvider sp)
    {
        var client = sp.GetRequiredService<IChatClient>();

        return new ChatClientAgent(
                client,
                name: "SupplierAgent",
                description: "Supplier parts agent",
                instructions: SupplierPartsInstructions,
                tools: [.. new Tools.PartSupplierSearch(sp.GetRequiredService<IConfiguration>()).AsAITools()])
            .AsBuilder()
            .UseOpenTelemetry("SupplierAgent", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();
    }

    public static AIAgent CreateCasePublisherAgent(IServiceProvider sp)
    {
        var client = sp.GetRequiredService<IChatClient>();
        var configuration = sp.GetRequiredService<IConfiguration>();

        var mcpClient = McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Name = "GitHub MCP",
            Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
            AdditionalHeaders = new AdditionalPropertiesDictionary<string>
            {
                { "Authorization", "Bearer " + configuration["GitHub:PersonalAccessToken"] }
            }
        })).ConfigureAwait(false).GetAwaiter().GetResult();

        var mcpTools = mcpClient.ListToolsAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        return new ChatClientAgent(
                client,
                name: "CasePublisherAgent",
                description: "Case publisher agent",
                instructions: "Use the support case to create a github issue on the repo: nissbran/azure-open-ai-demos.",
                tools: [.. mcpTools])
            .AsBuilder()
            .UseOpenTelemetry("CasePublisherAgent", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();
    }
}