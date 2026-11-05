# Demo3

Azure OpenAI and RAG integration with Microsoft.Extension.AI libraries. Function calling with Azure AI Search.

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
    "ApiKey": "  <your key>  "
  }
}
```

Then run the application.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.json` file or in environment variables.