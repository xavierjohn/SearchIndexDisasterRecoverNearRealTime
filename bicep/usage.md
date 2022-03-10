# Create the resources using ARM template.

## Login into your Azure Subscription
`az login`

Select your azure subscription if required.

` az account set -s $subscription `

## Create Resource group
` az deployment sub create --location WestUS2  --template-file .\weatherResourceGroups.bicep --parameters name=myweather `

## Create Resources
` az deployment group create --resource-group myweather-wus2 --template-file .\weatherResources.bicep --parameters name=myweather `