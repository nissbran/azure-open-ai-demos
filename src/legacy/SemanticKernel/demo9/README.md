# Demo9 - Multi-Agent Orchestration

Azure OpenAI and RAG integration using Semantic Kernel with multi-agent group chat orchestration. This demo showcases how multiple specialized agents can work together to answer complex queries about vehicle manufacturing, parts, and suppliers.

## Agents

### Bill of Materials (BoM) Agent
- Answers questions about truck bill of materials and parts requirements
- Searches through parts catalogs and assembly information
- Provides detailed component specifications and quantities

### Part Supplier Agent  
- Provides information about parts suppliers and availability
- Tracks supplier relationships and contact information
- Manages supplier performance and delivery data

### Vehicle Production Agent
- Tracks vehicle production data including VIN numbers, build dates, and models
- Monitors production status across different manufacturing lines and plants
- Provides vehicle specifications including engine types, colors, and options
- Supports queries about production timelines and manufacturing locations.

### Warehouse Agent
- Provides real-time warehouse inventory information and stock quantities
- Connects to SQL Server database to fetch current inventory levels, reserved quantities, and available stock
- Includes warehouse location information (warehouse ID, bin locations, contact details)
- Alerts on low stock levels and reorder points for critical parts
- Supports queries about part availability, stock levels, and warehouse locations

## To run

Use the instructions in the [main readme](../README.md) to deploy the infrastructure and set up environment variables or user secrets. It is also possible to use a local json file for configuration.


If you prefer, create a file called `appsettings.local.json` in the root of the project with the content:
```json
{
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "AzureOpenAI": {
    "Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "ApiKey": "  <your key>  ",
    "ChatModel": " <your chat model, ex gpt-4-with-assistants >   ",
    "EmbeddingModel": " <your embedding model, ex embedding-ada-002 >   "
  },
  "AzureAISearch": {
    "Endpoint": "https://   <your endpoint>   .search.windows.net",
    "ApiKey": "  <your key>  ",
    "RebuildIndex": "true"
  },
  "ConnectionStrings": {
    "WarehouseDatabase": "Server=(localdb)\\mssqllocaldb;Database=WarehouseDemo;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

Set `RebuildIndex` to `true` the first time you run the application to create and populate the search indexes for all agents.

**Note**: The Warehouse Agent will automatically fall back to simulated data if no SQL Server database is available. To use a real SQL Server database, configure the `ConnectionStrings:WarehouseDatabase` setting with your actual database connection string.

Then run the application.

## How It Works

The multi-agent system uses Semantic Kernel's orchestration capabilities to coordinate between specialized agents. When you ask a question, the system:

1. **Determines Relevance**: The orchestrator determines which agents are best suited to answer your question
2. **Agent Collaboration**: Multiple agents can collaborate on complex queries that span multiple domains
3. **RAG Integration**: Each agent uses Retrieval-Augmented Generation with Azure AI Search to access its specialized data
4. **Coordinated Response**: The orchestrator combines responses from multiple agents into a coherent answer

## Example Queries

- "What parts are needed for a Contoso Hauler 500 and who supplies them?"
- "Show me the production status of vehicles built in January 2024"
- "What is the VIN and build date for trucks that need brake system parts?"
- "Which supplier provides engines for the vehicles currently in production?"
- "How many V6 Intercooled Diesel engines do we have in stock?"
- "Show me all parts that are below reorder point in warehouse WH001"
- "What's the current inventory level for part number ENG001?"
- "Which warehouse has the most brake system parts available?"

The system can handle complex queries that require information from multiple agents, providing comprehensive answers about vehicle manufacturing, parts requirements, production tracking, and warehouse inventory management.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.


## To get tracing export to Aspire Dashboard

Start the Aspire Dashboard in standalone mode and run the application. The traces will be exported to the Aspire Dashboard.

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:9.3
```