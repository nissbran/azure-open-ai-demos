# Demo11 Chat UI

This frontend is the chat user interface for Demo11.

## What It Does

- Renders the chat experience in the browser
- Sends user messages to the Demo11 backend service
- Displays assistant responses returned by the backend

The frontend and backend together provide a simple end-to-end chat application.

## How to Start

1. Start the backend first from `src/demo11/agent`:

```sh
dotnet run --project Demo11-AgentFramework-BasicChat.csproj
```

2. Start the UI from `src/demo11/frontend`:

```sh
npm install
npm run dev
```

3. Open `http://localhost:3000`.

Or run the full Demo11 solution (backend + frontend) from the repo root:

```sh
dotnet run --project src/demo11/aspire/Demo11.AspireHost.csproj
```