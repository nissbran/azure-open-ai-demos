# McpToolServer

Custom Model Context Protocol (MCP) server providing tools for Star Wars data lookups, used by MCP-enabled demos such as Demo6.

## Tools

- **VehicleSearchTool**: Searches for Star Wars vehicles using Azure AI Search
- **ShipTool**: Retrieves Star Wars ship information from the SWAPI

## To run

If you prefer, create a file called `appsettings.local.json` in the root of the project with the content:
```json
{
  "Serilog": {
    "MinimumLevel": "Information"
  },
  "AzureAISearch": {
    "Endpoint": "https://   <your endpoint>   .search.windows.net",
    "ApiKey": "  <your key>  "
  },
  "Swapi": {
    "Url": "https://swapi.dev/api/"
  }
}
```

Then run the application:
```bash
cd src/McpToolServer
dotnet run
```

The server starts on `http://localhost:5195` by default and exposes an MCP endpoint at `/mcp`.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.
