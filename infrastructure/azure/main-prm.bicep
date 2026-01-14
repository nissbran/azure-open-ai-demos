targetScope = 'subscription'

param location string = deployment().location
param name string
param publisherEmail string
param publisherName string

var apimName = 'apim-${name}'

resource group 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${name}-demo'
  location: location
}


module uai 'modules/uai-identity.bicep' = {
  name: 'uai-identity-deployment'
  scope: group
  params: {
    identityName: 'uai-apim-${name}'
    location: location
  }
}

module entraApps 'modules/mcp-entra-apps.bicep' = {
  name: 'mcp-entra-apps-deployment'
  scope: group
  params: {
    mcpAppUri: 'https://${apimName}.azure-api.net/graph/mcp'
    userAssignedIdentityPrincipalId: uai.outputs.identityPrincipalId
  }
}

module apim 'modules/apim-prm-api.bicep' = {
  name: 'apim-deployment'
  scope: group
  params: {
    apimServiceName: apimName
    location: location
    publisherEmail: publisherEmail
    publisherName: publisherName
    userAssignedIdentityId: uai.outputs.identityId
    userAssignedIdentityClientId: uai.outputs.identityClientId
    mcpOBOClientId: entraApps.outputs.mcpAppId
  }
}
