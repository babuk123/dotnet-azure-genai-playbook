// Azure Container Apps: a worker that scales 0 → 10 on Service Bus queue length (KEDA), using managed identity
param location string = resourceGroup().location
param image string = 'myacr.azurecr.io/statement-worker:1.0.0'
param serviceBusNamespace string

resource env 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-banking'
  location: location
  properties: {}
}

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-statement-worker'
  location: location
}

resource worker 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'statement-worker'
  location: location
  identity: { type: 'UserAssigned', userAssignedIdentities: { '${identity.id}': {} } }
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      registries: [ { server: 'myacr.azurecr.io', identity: identity.id } ]
    }
    template: {
      containers: [
        {
          name: 'worker'
          image: image
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ServiceBus__FullyQualifiedNamespace', value: '${serviceBusNamespace}.servicebus.windows.net' }
            { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 10
        rules: [
          {
            name: 'queue-length'
            custom: {
              type: 'azure-servicebus'
              identity: identity.id
              metadata: { namespace: serviceBusNamespace, queueName: 'statements', messageCount: '20' }
            }
          }
        ]
      }
    }
  }
}
