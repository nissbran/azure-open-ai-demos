# Demo4

Azure OpenAI and function calling using an external MCP server.

## To run

Create a file called 'appsettings.local.json' in the root of the project with the content: 
```json
{
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "AzureOpenAI": {
    "Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "ApiKey": "  <your key>  ",
    "ChatModel": " <your chat model, ex gpt-4-with-assistants >   "
  },
  "McpServer": {
    "BaseUrl": "http://localhost:5195"
  }
}
```

Before starting the application, make sure the mcp server is running. You can do this by starting the `McpToolServer`.

Then run the application.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.