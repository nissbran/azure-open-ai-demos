# Demo10

Azure OpenAI and RAG integration using Semantic Kernel using agent group chat..

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [Functions Core Tools](https://github.com/Azure/azure-functions-core-tools)
- [Docker](https://www.docker.com/get-started/)
- An foundry resource. Follow the instructions [here](../../infrastructure/azure/ReadMe.md) to set up the infrastructure.

## To run

Use the instructions in the [main readme](../README.md) to deploy the infrastructure and set up environment variables or user secrets. It is also possible to use a local json file for configuration.

If you prefer, update the `local.settings.json` file with your configuration values:
```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "DURABLE_TASK_SCHEDULER_CONNECTION_STRING": "Endpoint=http://localhost:8080;TaskHub=taskhub1;Authentication=None",
    "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:4317",
    "OTEL_SERVICE_NAME": "Demo12-Alarm-Analytics-Durable-Agents",
    "AzureOpenAI__Endpoint": "https://   <your endpoint>  .openai.azure.com/",
    "AzureOpenAI__ApiKey": "  <your key>  ",
    "AzureOpenAI__ChatModel": " <your chat model, ex gpt-4-1 >   ",
    "AzureAISearch__Endpoint": "https://   <your endpoint>   .search.windows.net",
    "AzureAISearch__ApiKey": "  <your key>  "
  }
}
```

To run the application, start the Azurite storage emulator, the Durable Task Scheduler and the Aspire Dashboard as docker containers:

```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 -d --name azurite mcr.microsoft.com/azure-storage/azurite:latest
docker run -p 8080:8080 -p 8082:8082 -d --name durable-task-scheduler mcr.microsoft.com/dts/dts-emulator:latest
docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then run the application.

## Tracing and monitoring

To see the traces, open the Aspire Dashboard at `http://localhost:18888`. You should see the traces from the application.
To see the workflow execution, open the Durable Task Scheduler dashboard at `http://localhost:8082`. You should see the workflow executions there.