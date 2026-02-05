# Demo11-AgentFramework-BasicChat

This project demonstrates a basic chat application using the agent framework, similar to Demo 10, but with only the basic chat functionality as shown in Demo 1.

## Features
- Basic chat interface
- Utilizes the agent framework for extensibility

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Azure OpenAI access and configuration. See the [main README](../../README.md) for setup instructions.

## How to Run

Use the instructions in the [main README](../../README.md) to configure your Azure OpenAI credentials.

1. Build the project:
   ```sh
   dotnet build
   ```
2. Run the project:
   ```sh
   dotnet run --project src/demo11/Demo11-AgentFramework-BasicChat.csproj
   ```

## Project Structure
- `Program.cs`: Entry point with basic chat logic

## Requirements
- .NET 10 SDK

---

For more advanced agent orchestration and analytics, see [Demo 10](../demo10).
