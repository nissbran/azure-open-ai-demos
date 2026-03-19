# Demo11 Agent Backend

This backend powers the Demo11 chat experience.

## What It Does

- Accepts chat messages from the frontend UI
- Runs the Microsoft Agent Framework chat logic
- Returns assistant responses to the UI

The backend is intentionally minimal and focused on basic chat behavior.

## How to Start

From `src/demo11/agent`:

```sh
dotnet run --project Demo11-AgentFramework-BasicChat.csproj
```

To run the full Demo11 solution (backend + frontend) from the Aspire host:

```sh
dotnet run --project src/demo11/aspire/Demo11.AspireHost.csproj
```
