# azure-open-ai-demos

Demo repo for showcasing how to use Azure OpenAI services with various frameworks and patterns.

## Prerequisites

- An Azure subscription with access to Azure AI Foundry and Azure AI Search.
- .NET 9 SDK installed.
- An IDE such as Visual Studio or Visual Studio Code.
- Azure CLI installed. [Install Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)

## Getting Started

To use the demos, you need to set up the required infrastructure and configure the applications.
1. **Deploy Infrastructure**: Follow the instructions in [Deploy Infrastructure](infrastructure/azure/ReadMe.md) to deploy the necessary Azure resources using Bicep.
2. **Configure Environment Variables (choose one)**:
-  Create a `.env` file in the root of the repo based on the `.env.example` file. Populate it with your Azure OpenAI and Azure AI Search credentials. 
- **Or** Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to manage sensitive configuration data during development. Replace the placeholders with your actual values (the id is the same for all demos):
```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "<your endpoint>" --id ca64dc64-4faf-43c1-87c2-3854d28b0ccd
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your api key>" --id ca64dc64-4faf-43c1-87c2-3854d28b0ccd
dotnet user-secrets set "AzureOpenAI:ChatModel" "<your chat model>" --id ca64dc64-4faf-43c1-87c2-3854d28b0ccd
dotnet user-secrets set "AzureOpenAI:EmbeddingModel" "<your embedding model>" --id ca64dc64-4faf-43c1-87c2-3854d28b0ccd
dotnet user-secrets set "AzureAISearch:Endpoint" "<your endpoint>" --id ca64dc64-4faf-43c1-87c2-3854d28b0ccd
dotnet user-secrets set "AzureAISearch:ApiKey" "<your api key>" --id ca64dc64-4faf-43c1-87c2-3854d28b0ccd
```
3. **Run a Demo**: Navigate to the desired demo folder (e.g., `src/demo1`) and run the application using the .NET CLI:
```bash
cd src/demo1
dotnet run
```
## Demos

### Core Azure OpenAI Patterns
* **[Demo1](src/demo1)**: Basic Azure OpenAI ChatCompletion - Simple chat interface using the Azure OpenAI SDK
* **[Demo2](src/demo2)**: Function Calling + RAG - Demonstrates function calling with Retrieval Augmented Generation using Azure AI Search
* **[Demo3](src/demo3)**: Microsoft.Extensions.AI + RAG - Same functionality as Demo2 but using the new Microsoft.Extensions.AI libraries for .NET

### Semantic Kernel Demos
* **[Demo4](src/demo4)**: Semantic Kernel with RAG - Plugin-based architecture using Semantic Kernel with Azure AI Search integration
* **[Demo5](src/demo5)**: Semantic Kernel with Agents - Multi-agent conversation system with OpenTelemetry observability
* **[Demo6](src/demo6)**: Semantic Kernel + Custom MCP - Integration with Model Context Protocol using a custom MCP server

### MCP (Model Context Protocol) Integration
* **[Demo7](src/demo7)**: GitHub MCP Server - Function calling using the official GitHub MCP server for repository interactions
* **[Demo8](src/demo8)**: Azure Foundry Agent Service - Advanced agent service with MCP server integration on Azure Foundry

### Utility Projects
* **[indexer](src/indexer)**: SWAPI Data Indexer - Indexes Star Wars API data into Azure AI Search for use by the RAG demos
* **[McpToolServer](src/McpToolServer)**: Custom MCP Server - Example Model Context Protocol server implementation ([Learn about MCP](https://modelcontextprotocol.io))

**Disclaimer**: This is a demo repo and not intended for production use.