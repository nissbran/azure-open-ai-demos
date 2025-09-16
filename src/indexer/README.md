# Swapi Indexer

Application used to index the Swapi api with Azure OpenAI and Azure AI Search.

## To run

Use the instructions in the [main readme](../README.md) to deploy the infrastructure and set up environment variables or user secrets. It is also possible to use a local json file for configuration.


If you prefer, create a file called `appsettings.local.json` in the root of the project with the content: 
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "ApiKey": "  <your key>  ",
    "EmbeddingModel": " <your embedding model, ex embedding-ada-002- > "
  },
  "AzureAISearch": {
    "Endpoint": "https://   <your endpoint>   .search.windows.net",
    "ApiKey": "  <your key>  "
  }
}
```

Then run the application.