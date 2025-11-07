using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Serilog;

namespace Demo10;

public class AgentProcess
{
    private readonly IChatClient _chatClient;
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    private McpClient _gitHubMcpClient;
    private AIAgent _analyticsAgent;
    private AIAgent _supplierAgent;
    private AIAgent _supportCaseAgent;
    private AIAgent _casePublishAgent;

    public AgentProcess(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var model = configuration["AzureOpenAI:ChatModel"] ?? throw new ArgumentNullException(nameof(configuration), "ChatModel configuration is missing.");
        var apiKey = configuration["AzureOpenAI:ApiKey"] ?? throw new ArgumentNullException(nameof(configuration), "ApiKey configuration is missing.");
        var endpoint = configuration["AzureOpenAI:Endpoint"] ?? throw new ArgumentNullException(nameof(configuration), "Endpoint configuration is missing.");

        _loggerFactory = loggerFactory;
        _configuration = configuration;
        
        _chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey))
            .GetChatClient(model)
            .AsIChatClient() // Converts a native OpenAI SDK ChatClient into a Microsoft.Extensions.AI.IChatClient
            .AsBuilder()
            .UseFunctionInvocation()
            .UseOpenTelemetry(sourceName: "ChatClient", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the chat client level
            .Build();
    }
    
    public async Task ConfigureWorkflowAsync()
    {

        _analyticsAgent = new ChatClientAgent(
                _chatClient,
                name: "AlarmAnalyticsAgent",
                description: "Alarm analytics agent",
                instructions: AnalyticsInstructions)
            .AsBuilder()
            .UseOpenTelemetry("AlarmAnalyticsAgent", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();

        _supplierAgent = new ChatClientAgent(
                _chatClient,
                name: "SupplierAgent",
                description: "Supplier parts agent",
                instructions: SupplierPartsInstructions,
                tools: [.. new PartSupplierSearchPlugin(_configuration).AsAITools()])
            .AsBuilder()
            .UseOpenTelemetry("SupplierAgent", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();

        _supportCaseAgent = new ChatClientAgent(
                _chatClient,
                name: "SummaryAgent",
                description: "Summary agent",
                instructions: "Please create a support case for this for the current information."
            )
            .AsBuilder()
            .UseOpenTelemetry("SummaryAgent", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();

        
        _gitHubMcpClient = await McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Name = "GitHub MCP",
            Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
            AdditionalHeaders = new AdditionalPropertiesDictionary<string>
            {
                { "Authorization", "Bearer " + _configuration["GitHub:PersonalAccessToken"] }
            }
        }));

        var mcpTools = await _gitHubMcpClient.ListToolsAsync().ConfigureAwait(false);
        
        _casePublishAgent = new ChatClientAgent(
                _chatClient,
                name: "CasePublishAgent",
                description: "Case publish agent",
                instructions: "Use the support case to create a github issue on the repo: nissbran/azure-open-ai-demos.",
                tools: [.. mcpTools]
            )
            .AsBuilder()
            .UseOpenTelemetry("CasePublishAgent", configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();

    }

    public async Task<List<ChatMessage>> RunAsync(string alarmContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = AgentWorkflowBuilder.BuildSequential(_analyticsAgent, _supplierAgent, _supportCaseAgent, _casePublishAgent);
            var result = new List<ChatMessage>();

            var run = await InProcessExecution.RunAsync(workflow, alarmContent, cancellationToken: cancellationToken);
            
            foreach (var evt in run.NewEvents)
            {
                if (evt is AgentRunUpdateEvent e)
                {
                    //Console.Write(e.Update.Text);
                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                    {
                        Log.Information("[Calling function '{CallName}' with arguments: {Serialize}]", call.Name, JsonSerializer.Serialize(call.Arguments));
                    }
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    result = output.As<List<ChatMessage>>();
                }
            }

            return result;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to execute ");
            throw;
        }
    }
    
    
    private const string AnalyticsInstructions = """
                                                 You are an expert in analyzing alarm data from trucks. Your task is to help users understand and interpret alarm analytics data, identify patterns, and provide insights based on the data provided.

                                                 Instructions
                                                 - The alarm will be provided as a JSON.
                                                 - Analyze the alarm data to identify trends, anomalies, and potential issues.
                                                 - Extract the part information from the alarm data.

                                                 """;

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

}

internal sealed class PartSupplierSearchPlugin()
{
    private readonly SearchClient _searchClient;

    public PartSupplierSearchPlugin(IConfiguration configuration) : this()
    {
        _searchClient = new SearchClient(
            new Uri(configuration["AzureAISearch:Endpoint"]), 
            "supplier-parts-index", 
            new AzureKeyCredential(configuration["AzureAISearch:ApiKey"]));
    }
    
    [Description("Search supplier parts. It does not contain what parts goes where. The data contains part number, part name, part type, category, supplier, unit cost, lead time in weeks, country of origin, warranty period in months, and stock status.")]
    public async Task<List<SupplierPartSearchResult>> SearchForSupplierParts(
        [Description("Seach query for supplier parts")]string searchQuery)
    {
        try
        {
            Log.Verbose("Searching for parts with query {SearchQuery}", searchQuery);
            
            var searchResponse = await _searchClient.SearchAsync<SupplierPartSearchResult>(searchQuery, new SearchOptions()
            {
                QueryType = SearchQueryType.Semantic,
                Size = 5,
                VectorSearch = new VectorSearchOptions
                {
                    Queries =
                    {
                        new VectorizableTextQuery(searchQuery)
                        {
                            KNearestNeighborsCount = 5,
                            Fields = { "summary_vector" }
                        }
                    }
                },
                SemanticSearch = new SemanticSearchOptions()
                {
                    SemanticConfigurationName = "default",
                    QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive),
                    QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
                    ErrorMode = SemanticErrorMode.Partial,
                    MaxWait = TimeSpan.FromSeconds(5)
                }
            });

            var result = searchResponse.Value.GetResults().ToList();
        
            Log.Verbose("Number of search results: {Count}", result.Count);
        
            if (result.Count == 0)
            {
                return [];
            }

            var json = JsonSerializer.Serialize(result.Select(searchResult => searchResult.Document).ToList());
            
            Log.Verbose("Search result: {Summary}", json);
            
            return result.Select(searchResult => searchResult.Document).ToList();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to search for bill of materials");
            throw;
        }
    }

    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(SearchForSupplierParts);
    }
}
public record SupplierPartSearchResult(
    string part_number,
    string part_name,
    string part_type,
    string category,
    string supplier,
    decimal unit_cost,
    int lead_time_weeks,
    string country_of_origin,
    int warranty_period_months,
    string stock_status
);