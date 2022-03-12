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

module allResources 'resources.bicep' = [for location in locations : {
  name:'allResources'
  scope:resourceGroup('${name}-${location.shortRegion}')
  params: {
    name: name
    shortRegion: location.shortRegion
    location: location.region
  }
}]

resource primaryEventHubNamespace 'Microsoft.EventHub/namespaces@2021-11-01' existing = {
  name: '${name}-wus2'
}

resource disasterRecoveryConfigs 'Microsoft.EventHub/namespaces/disasterRecoveryConfigs@2021-11-01' = {
  name: name
  parent: primaryEventHubNamespace
  properties: {
    partnerNamespace: resourceId('Microsoft.EventHub/namespaces', '${name}-eus2')
  }
  dependsOn: [
    allResources
  ]
}
