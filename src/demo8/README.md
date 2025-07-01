# Demo8

Azure Foundry Agent Service with MCP server integration.

## To run

First deploy a Azure Foundry Instance and create a project. 

Create a file called 'appsettings.local.json' in the root of the project with the content: 
```json
"Serilog": {
    "MinimumLevel": "Information"
},
"AzureOpenAI": {
    "Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "ApiKey": "  <your key>  ",
    "ChatModel": " <your chat model, ex gpt-4-with-assistants >   "
},
"AgentService": {
    "AgentId": " <your agent id> ",
    "Endpoint": "https:// <your instance> .services.ai.azure.com/api/projects/ <your project> /",
    "McpBaseUrl": "https:// <your site> .azurewebsites.net/sse"
}
```

Then run the application.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.