targetScope = 'subscription'

param location string = deployment().location
param searchLocationOverride string = 'westeurope'
param name string

resource group 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${name}-demo'
  location: location
}

module openai 'modules/azure-openai.bicep' = {
  name: 'openai'
  scope: group
  params: {
    nameSuffix: name
    location: location
  }
}

module search 'modules/search-services.bicep' = {
  name: 'search'
  scope: group
  params: {
    nameSuffix: name
    location: searchLocationOverride
  }
}

