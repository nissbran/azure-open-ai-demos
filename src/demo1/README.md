# Demo1

Pure open AI chatbot with no RAG integration.

## To run

Create a file called 'appsettings.local.json' in the root of the project with the content: 
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