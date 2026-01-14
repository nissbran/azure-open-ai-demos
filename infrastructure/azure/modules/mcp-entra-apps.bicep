extension 'br:mcr.microsoft.com/bicep/extensions/microsoftgraph/v1.0:1.0.0'

@description('The name of the MCP Entra application')
param mcpGraphServerAppUniqueName string = 'mcp-graph-server-app-22'

@description('The name of the MCP Inspector client application')
param mcpInspectorClientAppUniqueName string = 'mcp-inspector-client-app-22'

@description('The name of the MCP Custom client application')
param mcpCustomClientAppUniqueName string = 'mcp-custom-client-app-22'

@description('The MCP App URI used in APIM for token audience validation')
param mcpAppUri string

@description('The principal id of the user-assigned managed identity')
param userAssignedIdentityPrincipalId string

resource inspectorClientApp 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'MCP Inspector Client App'
  uniqueName: mcpInspectorClientAppUniqueName
}

resource customClientApp 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'MCP Custom Client App'
  uniqueName: mcpCustomClientAppUniqueName
}

resource mcpGraphServerApp 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'MCP Graph Server App'
  uniqueName: mcpGraphServerAppUniqueName
  identifierUris: [
    mcpAppUri
  ]
  api: {
    oauth2PermissionScopes: [
      {
        id: guid(mcpGraphServerAppUniqueName, 'access_as_user')
        adminConsentDescription: 'Allows the application to access MCP resources on behalf of the signed-in user'
        adminConsentDisplayName: 'Access MCP resources'
        isEnabled: true
        type: 'User'
        userConsentDescription: 'Allows the app to access MCP resources on your behalf'
        userConsentDisplayName: 'Access MCP resources'
        value: 'access_as_user'
      }
    ]
    requestedAccessTokenVersion: 2
    knownClientApplications: [
      inspectorClientApp.appId
      customClientApp.appId
    ]
    preAuthorizedApplications: [
      {
        appId: 'aebc6443-996d-45c2-90f0-388ff96faa56' // Visual Studio Code
        delegatedPermissionIds: [
          guid(mcpGraphServerAppUniqueName, 'access_as_user')
        ]
      }
    ]
  }
  requiredResourceAccess: [
    {
      resourceAppId: '00000003-0000-0000-c000-000000000000' // Microsoft Graph
      resourceAccess: [
        {
          id: 'e1fe6dd8-ba31-4d61-89e7-88639da4683d' // User.Read
          type: 'Scope'
        }
        {
          id: 'b4e74841-8e56-480b-be8b-910348b18b4c' // User.ReadWrite
          type: 'Scope'
        }
      ]
    }
  ]

  resource fic 'federatedIdentityCredentials@v1.0' = {
    name: '${mcpGraphServerAppUniqueName}/msiAsFic'
    description: 'Trust the user-assigned MI as a credential for the MCP app'
    audiences: [
      'api://AzureADTokenExchange'
    ]
    issuer: '${environment().authentication.loginEndpoint}${subscription().tenantId}/v2.0'
    subject: userAssignedIdentityPrincipalId
  }
}

resource mcpGraphServerAppServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: mcpGraphServerApp.appId
}

resource inspectorClientAppConfig 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'MCP Inspector Client App'
  uniqueName: mcpInspectorClientAppUniqueName

  requiredResourceAccess: [
    {
      resourceAppId: mcpGraphServerApp.appId
      resourceAccess: [
        {
          id: guid(mcpGraphServerAppUniqueName, 'access_as_user')
          type: 'Scope'
        }
      ]
    }
  ]
  spa: {
    redirectUris: [
      'http://localhost:6274/oauth/callback' // Default callback for MCP Inspector
      'http://localhost:6274/oauth/callback/debug' // Debug callback for MCP Inspector
    ]
  }
}

resource inspectorClientAppServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: inspectorClientAppConfig.appId
}

resource customClientAppConfig 'Microsoft.Graph/applications@v1.0' = {
  displayName: 'MCP Custom Client App'
  uniqueName: mcpCustomClientAppUniqueName

  requiredResourceAccess: [
    {
      resourceAppId: mcpGraphServerApp.appId
      resourceAccess: [
        {
          id: guid(mcpGraphServerAppUniqueName, 'access_as_user')
          type: 'Scope'
        }
      ]
    }
  ]
  publicClient: {
    redirectUris: [
      'http://localhost:8080/auth/callback' // Example redirect URI for public clients
    ]
  }
  // spa: {
  //   redirectUris: [
  //     'http://localhost:8080/auth/callback' // Example callback for MCP Custom Client
  //   ]
  // }
}

resource customClientAppServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: customClientAppConfig.appId
}

// Outputs
output mcpAppId string = mcpGraphServerApp.appId
output inspectorClientAppId string = inspectorClientApp.appId
output mcpAppUniqueName string = mcpGraphServerAppUniqueName
