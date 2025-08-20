param hubName string
param projectName string
// param principalId string
// param principalType string
param location string = resourceGroup().location

resource foundryHub 'Microsoft.CognitiveServices/accounts@2025-06-01' = {
  name: hubName
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'AIServices'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    apiProperties: {}
    customSubDomainName: hubName
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: []
      ipRules: []
    }
    allowProjectManagement: true
    defaultProject: projectName
    
    associatedProjects: [
      projectName
    ]
    publicNetworkAccess: 'Enabled'
  }
}

resource defenderForAi 'Microsoft.CognitiveServices/accounts/defenderForAISettings@2025-06-01' = {
  parent: foundryHub
  name: 'Default'
  properties: {
    state: 'Disabled'
  }
}

resource project 'Microsoft.CognitiveServices/accounts/projects@2025-06-01' = {
  parent: foundryHub
  name: projectName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    description: 'Default project created with the resource'
    displayName: projectName
  }
}

// resource azureAiUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
//   name: guid(foundryHub.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d'))
//   scope: foundryHub
//   properties: {
//     principalId: principalId
//     principalType: principalType
//     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '53ca6127-db72-4b80-b1b0-d745d6d5456d')
//   }
// }

resource deploymentLlm 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: foundryHub
  name: 'gpt-4.1'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4.1'
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 100
  }
  dependsOn: [
    project
  ]
}

resource deploymentEmbedding 'Microsoft.CognitiveServices/accounts/deployments@2025-06-01' = {
  parent: foundryHub
  name: 'text-embedding-3-small'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-3-small'
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 20
  }
  dependsOn: [
    deploymentLlm
    project
  ]
}

output foundryHubName string = foundryHub.name
output foundryHubId string = foundryHub.id
output foundryProjectName string = project.name
output openAiEndpoint string = 'https://${foundryHub.name}.cognitiveservices.azure.com'
