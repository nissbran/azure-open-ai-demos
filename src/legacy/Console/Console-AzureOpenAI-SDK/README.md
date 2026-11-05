# Demo1

Pure open AI chatbot with no RAG integration.

## To run

Use the instructions in the [main readme](../README.md) to deploy the infrastructure and set up environment variables or user secrets. It is also possible to use a local json file for configuration.


If you prefer, create a file called `appsettings.local.json` in the root of the project with the content:
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "ApiKey": "  <your key>  ",
    "ChatModel": " <your chat model, ex gpt-4-with-assistants >   "
  }
}
```

Then run the application.