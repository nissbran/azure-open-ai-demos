param nameSuffix string
param apimServiceName string = 'apim-${nameSuffix}'
param location string = resourceGroup().location
param publisherEmail string
param publisherName string
param userAssignedIdentityId string

resource apiManagementService 'Microsoft.ApiManagement/service@2024-10-01-preview' = {
  name: apimServiceName
  location: location
  sku: {
    name: 'BasicV2'
    capacity: 1
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentityId}': {}
    }
  }
}

output apiManagementServiceName string = apiManagementService.name
