# Deploy infrastructure

To deploy the infrastructure needed for the demos run the following command:

```bash
az deployment sub create -n deployment-se-foundry -l swedencentral --template-file main.bicep --parameters main.bicepparam
```

This will deploy the following resources:

- A resource group
- An Azure AI Foundry instance
- gpt-4.1 deployment
- text-embedding-3-small deployment
- An Azure AI Search instance


## Get the keys and endpoints

After deployment you can get the keys and endpoints from the Azure portal or by using the Azure CLI.
```bash
# Get resource group name
RESOURCE_GROUP=$(az deployment sub show -n deployment-se-foundry --query properties.outputs.resourceGroupName.value -o tsv)
# Get Azure OpenAI endpoint
AZURE_OPENAI_ENDPOINT=$(az cognitiveservices account show -n <your-cognitive-service-name> -g $RESOURCE_GROUP --query "properties.endpoint" -o tsv)
```

## Protected Resource Metadata (PRM) configuration

To configure PRM for the APIM MCP server with Microsoft Entra you need:

1. One API application registration (APIM MCP server)
    - Application ID URI: `https://<your-apim>/your-mcp/mcp`
    - Expose an API scope: `User.Read` or similar
2. One client application registration (VSCode, Mcp inspector or custom app) with:
    - Redirect URI for SPA: `http://localhost:6274`
    - API permissions to call the API application registration
    - Optional: Configure CORS on the APIM MCP server
3. An API that do on-behalf-of flow to the graph api
4. An APIM expose a API as a MCP server

