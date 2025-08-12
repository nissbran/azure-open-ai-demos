# azure-open-ai-demos

Demo repo for showcasing how to use Azure OpenAI services with various frameworks and patterns.

## Demos

### Core Azure OpenAI Patterns
* **Demo1**: Basic Azure OpenAI ChatCompletion - Simple chat interface using the Azure OpenAI SDK
* **Demo2**: Function Calling + RAG - Demonstrates function calling with Retrieval Augmented Generation using Azure AI Search
* **Demo3**: Microsoft.Extensions.AI + RAG - Same functionality as Demo2 but using the new Microsoft.Extensions.AI libraries for .NET

### Semantic Kernel Demos
* **Demo4**: Semantic Kernel with RAG - Plugin-based architecture using Semantic Kernel with Azure AI Search integration
* **Demo5**: Semantic Kernel with Agents - Multi-agent conversation system with OpenTelemetry observability
* **Demo6**: Semantic Kernel + Custom MCP - Integration with Model Context Protocol using a custom MCP server

### MCP (Model Context Protocol) Integration
* **Demo7**: GitHub MCP Server - Function calling using the official GitHub MCP server for repository interactions
* **Demo8**: Azure Foundry Agent Service - Advanced agent service with MCP server integration on Azure Foundry

### Utility Projects
* **indexer**: SWAPI Data Indexer - Indexes Star Wars API data into Azure AI Search for use by the RAG demos
* **McpToolServer**: Custom MCP Server - Example Model Context Protocol server implementation ([Learn about MCP](https://modelcontextprotocol.io))
* **ghcopilotsandbox**: GitHub Copilot Sandbox - Experimental project for GitHub Copilot features

**Disclaimer**: This is a demo repo and not intended for production use.