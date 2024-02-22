# Swapi Indexer

Application used to index the Swapi api with Azure OpenAI and Azure Search.

## To run

Create a file called 'appsettings.local.json' in the root of the project with the content: 
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "ApiKey": "  <your key>  ",
    "EmbeddingModel": " <your embedding model, ex embedding-ada-002- > "
  },
  "AzureCognitiveSearch": {
    "Endpoint": "https://   <your endpoint>   .search.windows.net",
    "ApiKey": "  <your key>  "
  }
}
```

Then run the application.