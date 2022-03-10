@description('Name that is the prefix of the resource group and resources.')
param name string

module weatherWest 'resources.bicep' = {
  name:'westResources'
  scope:resourceGroup('${name}-wus2')
  params: {
    name: name
    shortRegion: 'wus2'
    location: 'westus2'
  }
}


module weatherEast 'resources.bicep' = {
  name:'eastResources'
  scope:resourceGroup('${name}-eus2')
  params: {
    name: name
    shortRegion: 'eus2'
    location: 'eastus2'
  }
}
