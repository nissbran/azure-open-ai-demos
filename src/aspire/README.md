# Aspire

.NET Aspire AppHost for orchestrating and deploying the Azure OpenAI demo services. This project uses [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/aspire-overview) to provide a unified developer experience for running, observing, and deploying distributed demo applications.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/setup-tooling) installed

## To run

```bash
cd src/aspire
dotnet run
```

The Aspire dashboard will be available at `http://localhost:18888` for observing traces, logs, and metrics from the running services.
