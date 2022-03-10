@description('Name that is the prefix of the resource group and resources.')
param name string
var locations = [
  {
    'region' : 'westus2'
    'shortRegion': 'wus2'
  }
  {
    'region' : 'eastus2'
    'shortRegion': 'eus2'
  }
]

module resources 'resources.bicep' = [for location in locations : {
  name:'alResources'
  scope:resourceGroup('${name}-${location.shortRegion}')
  params: {
    name: name
    shortRegion: location.shortRegion
    location: location.region
  }
}]

/*
resource disasterRecoveryConfigs 'Microsoft.EventHub/namespaces/disasterRecoveryConfigs@2021-11-01' = {
  name: name
  parent: resources[0]/eventHubNamespace
  properties: {
    partnerNamespace: resourceId('Microsoft.EventHub/namespaces', '${name}-eus2')
  }
  dependsOn: [
    resources
  ]
}
*/
