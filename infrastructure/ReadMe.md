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