param name string
param location string
param shortRegion string
param tenantId string = subscription().tenantId

var resourceName = '${name}${shortRegion}'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  location:location
  name:resourceName
}  

resource storageBlobDataContributorRole 'Microsoft.Authorization/roleDefinitions@2018-01-01-preview' existing = {
  scope: subscription()
  // See https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-02-01' = {
  name: resourceName
  location:location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  scope: storageAccount
  name: guid(resourceGroup().id, managedIdentity.id, storageBlobDataContributorRole.id)
  properties: {
    roleDefinitionId: storageBlobDataContributorRole.id
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' = {
  name: resourceName
  location: location
  properties: {
    tenantId: tenantId
    accessPolicies:[
      {
        objectId: managedIdentity.properties.principalId
        tenantId: tenantId
        permissions: {
          secrets: [
            'list'
            'get'
          ] 
        }
      }
    ]
    sku: {
      family: 'A'
      name: 'standard'
    }
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: resourceName
  location: location
  sku: {
    name: 'D1'
    tier: 'Shared'
    size: 'D1'
    family: 'D'
    capacity: 0
  }
}

resource search 'Microsoft.Search/searchServices@2020-08-01' existing = {
  name: resourceName
}

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: resourceName
  location: location
  sku: {
    name:'Standard'
    tier:'Standard'
    capacity:1
  }
}

resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-11-01' = if (shortRegion == 'wus2') {
  name: 'city-temprature'
  parent: eventHubNamespace
  properties: {
    messageRetentionInDays: 1
    partitionCount: 2
  }
}

resource consumerGroupWus2 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2021-11-01' = if (shortRegion == 'wus2') {
  name: 'index-worker-wus2'
  parent: eventHub
}
resource consumerGroupEus2 'Microsoft.EventHub/namespaces/eventhubs/consumergroups@2021-11-01' = if (shortRegion == 'wus2') {
  name: 'index-worker-eus2'
  parent: eventHub
}
