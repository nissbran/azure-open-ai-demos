targetScope = 'subscription'

param location string = deployment().location
param name string

resource group 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${name}-demo'
  location: location
}

module foundry 'modules/foundry.bicep' = {
  name: 'foundry'
  scope: group
  params: {
    hubName: 'foundry-${name}'
    projectName: 'project-${name}'
    location: location
  }
}

module search 'modules/search-services.bicep' = {
  name: 'search'
  scope: group
  params: {
    nameSuffix: name
    location: location
  }
}

