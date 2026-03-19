# McpInstructionsSupportServer

Custom Model Context Protocol (MCP) server providing build pipeline setup tools and CI/CD prompt templates, used by MCP-enabled demos such as Demo6.

## Tools & Prompts

- **SetupBuildPipelineTool**: Provides instructions for setting up build pipelines
- **SetupInfrastructureAsCode**: Provides guidance on infrastructure as code setup
- **SetupCIPrompts**: MCP prompt templates for CI/CD pipeline configuration

## To run

If you prefer, create a file called `appsettings.local.json` in the root of the project with the content:
```json
{
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

Then run the application:
```bash
cd src/McpInstructionsSupportServer
dotnet run
```

The server starts and exposes an MCP endpoint at `/mcp`.

## To get verbose logging

Switch the `MinimumLevel` to `Verbose` in the `appsettings.local.json` file.
