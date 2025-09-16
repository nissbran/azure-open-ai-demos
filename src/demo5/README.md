# Demo5

Azure OpenAI and RAG integration using Semantic Kernel using agent group chat..

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

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.


## To get tracing export to Aspire Dashboard

Start the Aspire Dashboard in standalone mode and run the application. The traces will be exported to the Aspire Dashboard.

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:9.3
```