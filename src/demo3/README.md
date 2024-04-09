# Demo2

Azure OpenAI and RAG integration with Assistant Api. Function calling with Azure AI Search(Azure Cognitive Search).

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
    "ChatModel": " <your chat model, ex gpt-4-with-assistants >   ",
    "EmbeddingModel": " <your embedding model, ex embedding-ada-002 >   "
  },
  "AzureCognitiveSearch": {
    "Endpoint": "https://   <your endpoint>   .search.windows.net",
    "ApiKey": "  <your key>  "
  }
}
```

Then run the application.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.