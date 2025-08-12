# azure-open-ai-demos

Demo repo for showcasing how to use Azure OpenAI services with various frameworks and patterns.

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