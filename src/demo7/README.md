# Demo7

Azure OpenAI and function calling using the official github MCP server.

## To run

First generate a GitHub personal access token with at least metadata scope. You can do this by going to your GitHub account settings, then to Developer settings, and then to Personal access tokens.
You can add more scopes if you want to use the GitHub MCP server for other purposes.

Create a file called 'appsettings.local.json' in the root of the project with the content: 
```json
{
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "AzureOpenAI": {
    "Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "ApiKey": "< your api key >",
    "ChatModel": "< your chat model, ex gpt-4-with-assistants >"
  },
  "GitHub": {
    "PersonalAccessToken": "< github pat token >"
  }
}
```

Then run the application.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.