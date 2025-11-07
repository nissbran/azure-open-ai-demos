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