targetScope = 'subscription'
param name string
module WestRG 'resourceGroup.bicep' = {
  name: 'ResourceGroupWest'
  scope:subscription()
  params: {
    resourceGroupName: '${name}-wus2'
    resourceGroupLocation: 'westus2'
  }
}

module EastRG 'resourceGroup.bicep' = {
  name: 'ResourceGroupEast'
  scope:subscription()
  params: {
    resourceGroupName: '${name}-eus2'
    resourceGroupLocation: 'eastus2'
  }
}
