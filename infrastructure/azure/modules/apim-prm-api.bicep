param apimServiceName string
param location string = resourceGroup().location
param publisherEmail string
param publisherName string
param mcpOBOClientId string
param userAssignedIdentityId string
param userAssignedIdentityClientId string

@description('The pricing tier of this API Management service')
@allowed([
  'BasicV2'
  'StandardV2'
])
param apimSku string = 'BasicV2'

var apimGatewayUrl = 'https://${apimServiceName}.azure-api.net'

resource apim_service 'Microsoft.ApiManagement/service@2024-10-01-preview' = {
  name: apimServiceName
  location: location
  sku: {
    name: apimSku
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


// Named Values for MCP OBO configuration ----------------------------------------------------------------
//
resource named_value_APIMGatewayURL 'Microsoft.ApiManagement/service/namedValues@2024-10-01-preview' = {
  parent: apim_service
  name: 'APIMGatewayURL'
  properties: {
    displayName: 'APIMGatewayURL'
    value: apimGatewayUrl
    secret: false
  }
}

resource named_value_McpOBOClientId 'Microsoft.ApiManagement/service/namedValues@2024-10-01-preview' = {
  parent: apim_service
  name: 'McpOBOClientId'
  properties: {
    displayName: 'McpOBOClientId'
    value: mcpOBOClientId
    secret: false
  }
}

// resource named_value_McpOBOClientSecret 'Microsoft.ApiManagement/service/namedValues@2024-10-01-preview' = {
//   parent: apim_service
//   name: 'McpOBOClientSecret'
//   properties: {
//     displayName: 'McpOBOClientSecret'
//     value: mcpOBOClientSecret
//     secret: true
//   }
// }

resource named_Value_APIMUserAssignedManagedIdentityId 'Microsoft.ApiManagement/service/namedValues@2024-10-01-preview' = {
  parent: apim_service
  name: 'APIMUserAssignedManagedIdentityClientId'
  properties: {
    displayName: 'APIMUserAssignedManagedIdentityClientId'
    value: userAssignedIdentityClientId
    secret: false
  }
}

resource named_value_McpTenantId 'Microsoft.ApiManagement/service/namedValues@2024-10-01-preview' = {
  parent: apim_service
  name: 'McpTenantId'
  properties: {
    displayName: 'McpTenantId'
    value: subscription().tenantId
    secret: false
  }
}
//
// End Named Values for MCP OBO configuration ----------------------------------------------------------

// MCP Server definition infront of ms-graph API
resource graph_api_mcp 'Microsoft.ApiManagement/service/apis@2024-10-01-preview' = {
  parent: apim_service
  name: 'graph'
  properties: {
    displayName: 'graph'
    subscriptionRequired: false
    path: 'graph'
    protocols: [
      'https'
    ]
    type: 'mcp'
    mcpTools: [
      {
        name: graph_api_get_me_operation.name
        operationId: graph_api_get_me_operation.id
        description: graph_api_get_me_operation.properties.description
      }
    ]
  }
}

resource graph_api_mcp_policy 'Microsoft.ApiManagement/service/apis/policies@2024-10-01-preview' = {
  parent: graph_api_mcp
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('apim-policies/mcp-obo-front-auth.cshtml')
  }
  dependsOn: [
    named_value_APIMGatewayURL
    named_value_McpOBOClientId
    named_value_McpTenantId
  ]
}

// Add the API the calls Microsoft Graph. Backend for the MCP API
resource graph_api 'Microsoft.ApiManagement/service/apis@2024-06-01-preview' = {
  parent: apim_service
  name: 'ms-graph'
  properties: {
    displayName: 'API that calls Microsoft Graph'
    subscriptionRequired: false
    serviceUrl: 'https://graph.microsoft.com/v1.0/'
    path: 'ms-graph'
    protocols: [
      'https'
    ]
    isCurrent: true
  }
}

resource graph_api_get_me_operation 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  parent: graph_api
  name: 'get-me'
  properties: {
    displayName: 'Gets the current user from Microsoft Graph'
    description: 'Gets the current user from Microsoft Graph'
    method: 'GET'
    urlTemplate: '/me'
  }
}

resource get_me_policy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-10-01-preview' = {
  parent: graph_api_get_me_operation
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('apim-policies/mcp-obo-backend-uai-cached-auth.cshtml')
  }
  dependsOn: [
    named_value_APIMGatewayURL
    named_value_McpOBOClientId
    named_value_McpTenantId
    named_Value_APIMUserAssignedManagedIdentityId
  ]
}

// Add the .well-known/oauth-protected-resource API for PRM
resource prm_well_known_discovery_api 'Microsoft.ApiManagement/service/apis@2024-10-01-preview' = {
  parent: apim_service
  name: 'prm-well-known-discovery'
  properties: {
    displayName: 'Protected Resource Metadata'
    subscriptionRequired: false
    path: '/.well-known/oauth-protected-resource'
    protocols: [
      'https'
    ]
    isCurrent: true
  }
}

resource graph_mcp_discovery_endpoint 'Microsoft.ApiManagement/service/apis/operations@2024-06-01-preview' = {
  parent: prm_well_known_discovery_api
  name: 'graph-mcp'
  properties: {
    displayName: 'graph mcp discovery endpoint'
    method: 'GET'
    urlTemplate: '/graph/mcp'
  }
}

resource prm_discovery_graph_policy 'Microsoft.ApiManagement/service/apis/operations/policies@2024-10-01-preview' = {
  parent: graph_mcp_discovery_endpoint
  name: 'policy'
  properties: {
    format: 'rawxml'
    value: loadTextContent('apim-policies/mcp-obo-prm-discovery.cshtml')
  }
  dependsOn: [
    named_value_APIMGatewayURL
    named_value_McpOBOClientId
    named_value_McpTenantId
  ]
}

output apiManagementServiceName string = apim_service.name
output apiManagementServiceId string = apim_service.id
output apimGatewayUrl string = apimGatewayUrl
