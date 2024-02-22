param nameSuffix string
param searchName string = 'search-${nameSuffix}'
param location string = resourceGroup().location

resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    disableLocalAuth: false
    publicNetworkAccess: 'enabled'
    // authOptions: {
    //   aadOrApiKey: {
    //       aadAuthFailureMode: 'http401WithBearerChallenge'
    //   }
    // }
    semanticSearch: 'free'
  }
  sku:{
    name: 'basic'
  }
}
