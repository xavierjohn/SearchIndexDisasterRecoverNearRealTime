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
  kind: 'windows'
  sku: {
    name: 'S1'
  }
}

resource appService 'Microsoft.Web/sites@2020-06-01' = {
  name: resourceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true   
    siteConfig: {
      ftpsState: 'Disabled'
      netFrameworkVersion: 'v6.0'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          'name': 'AZURE_CLIENT_ID'
          'value': managedIdentity.properties.clientId
        } 
        {
          'name': 'environmentSettings:region'
          'value': shortRegion
        } 
        {
          'name': 'eventHubSettings:CommonBlobStorageUri'
          'value': 'https://${resourceName}${environment().suffixes.storage}/'
        } 
        {
          'name': 'eventHubSettings:eventHubNamespace'
          'value': '${name}.servicebus.windows.net'
        } 
        {
          'name': 'eventHubSettings:regionSpecificBlobStorageUri'
          'value': 'https://${resourceName}${environment().suffixes.storage}/'
        }
        {
          'name': 'keyVaultSettings:keyVaultName'
          'value': resourceName
        }
        {
          'name': 'searchServiceSettings:ServiceUri'
          'value': 'https://${resourceName}.search.windows.net'
        }
      ]
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  
}

resource search 'Microsoft.Search/searchServices@2020-08-01' existing = {
  name: resourceName
}

resource secret 'Microsoft.KeyVault/vaults/secrets@2021-04-01-preview' = {
  parent: keyVault
  name: 'SearchServiceKey'
  properties: {
    value: search.listAdminKeys().primaryKey
  }
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

