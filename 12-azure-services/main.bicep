// App Service + Key Vault + Service Bus, connected with a managed identity (no secrets in config)
param location string = resourceGroup().location
param appName string = 'orders-${uniqueString(resourceGroup().id)}'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  sku: { name: 'P0v3' }
  kind: 'linux'
  properties: { reserved: true }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'KeyVaultUri', value: kv.properties.vaultUri }
        { name: 'ServiceBus__FullyQualifiedNamespace', value: '${sb.name}.servicebus.windows.net' }
      ]
    }
  }
}

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: take('kv-${appName}', 24)
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    enablePurgeProtection: true
  }
}

resource sb 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: 'sb-${appName}'
  location: location
  sku: { name: 'Standard' }
  properties: { disableLocalAuth: true }   // Entra ID only, no connection strings
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: sb
  name: 'orders'
  properties: { maxDeliveryCount: 5, deadLetteringOnMessageExpiration: true }
}

// RBAC: the app's identity can read secrets and send/receive messages
var kvSecretsUser = '4633458b-17de-408a-b874-0445c86b69e6'
var sbDataOwner = '090c5cfd-751d-490a-894a-3ce6f1109419'

resource kvRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, app.id, kvSecretsUser)
  properties: {
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUser)
  }
}

resource sbRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: sb
  name: guid(sb.id, app.id, sbDataOwner)
  properties: {
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', sbDataOwner)
  }
}

output appUrl string = 'https://${app.properties.defaultHostName}'
