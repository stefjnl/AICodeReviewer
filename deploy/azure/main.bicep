// ============================================================================
// CodeGuard - Azure Container Apps Deployment
// ============================================================================
// This template creates all infrastructure needed to run CodeGuard in Azure:
// - Container Registry (stores Docker images)
// - Key Vault (stores API keys securely)
// - Managed Identity (secure authentication without passwords)
// - Container App (runs your application)
// ============================================================================

// Parameters - values you provide during deployment
@description('Azure region for all resources')
param location string = 'westeurope'

@description('Base name for all resources')
param appName string = 'codeguard'

@description('OpenRouter API key (will be stored securely in Key Vault)')
@secure()
param openRouterApiKey string

@description('Docker image tag to deploy')
param imageTag string = 'latest'

// Variables - computed values
var resourcePrefix = '${appName}-${uniqueString(resourceGroup().id)}'
var containerRegistryName = replace(resourcePrefix, '-', '') // Registry names cannot contain hyphens
var keyVaultName = '${appName}-ss-kv'

// ============================================================================
// CONTAINER REGISTRY
// ============================================================================
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Basic' // €5/month - sufficient for development/small projects
  }
  properties: {
    adminUserEnabled: false // Use Managed Identity instead of username/password
    publicNetworkAccess: 'Enabled'
  }
}

// ============================================================================
// KEY VAULT - Secure storage for secrets
// ============================================================================
resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true // Modern role-based access control

    // Security settings
    enableSoftDelete: true // Deleted secrets recoverable for 90 days
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true // Cannot permanently delete (prevents accidents)
  }
}

// Store OpenRouter API key as a secret
resource openRouterSecret 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
  parent: keyVault
  name: 'OpenRouterApiKey'
  properties: {
    value: openRouterApiKey
  }
}

// ============================================================================
// MANAGED IDENTITY - "Service account" for CodeGuard
// ============================================================================
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${resourcePrefix}-identity'
  location: location
}

// Grant permission to pull Docker images from Container Registry
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, managedIdentity.id, 'acrpull')
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d'
    ) // AcrPull role
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Grant permission to read secrets from Key Vault
resource keyVaultSecretUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, managedIdentity.id, 'secretuser')
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    ) // Key Vault Secrets User role
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================================
// CONTAINER APP ENVIRONMENT - Hosting platform
// ============================================================================
resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${resourcePrefix}-env'
  location: location
  properties: {
    // Azure manages networking, logging, scaling infrastructure
  }
}

// ============================================================================
// CONTAINER APP - Your CodeGuard application
// ============================================================================

/*
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: '${resourcePrefix}-app'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      ingress: {
        external: true // Accessible from internet
        targetPort: 8097 // Your app's port (matches current setup)
        transport: 'auto' // Supports both HTTP/1 and HTTP/2
        allowInsecure: false // Enforce HTTPS
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: managedIdentity.id // Authenticate using Managed Identity
        }
      ]
      secrets: [
        {
          name: 'openrouter-api-key'
          keyVaultUrl: openRouterSecret.properties.secretUri // Pull from Key Vault
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'codeguard'
          image: '${containerRegistry.properties.loginServer}/codeguard:${imageTag}'
          resources: {
            cpu: json('0.5') // 0.5 CPU cores
            memory: '1Gi' // 1 GB RAM
          }
          env: [
            {
              name: 'OpenRouter__ApiKey'
              secretRef: 'openrouter-api-key' // ASP.NET Core config: OpenRouter:ApiKey
            }
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8097' // Explicit port binding
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1 // Always at least 1 instance
        maxReplicas: 3 // Scale up to 3 under load
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '10' // Scale up when >10 concurrent requests per instance
              }
            }
          }
        ]
      }
    }
  }
  dependsOn: [
    acrPullRole
    keyVaultSecretUserRole
  ]
}

*/

// ============================================================================
// OUTPUTS - Information you'll need after deployment
// ============================================================================
//output containerAppUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output containerRegistryName string = containerRegistry.name
output managedIdentityClientId string = managedIdentity.properties.clientId
output keyVaultName string = keyVault.name
output resourceGroupName string = resourceGroup().name
