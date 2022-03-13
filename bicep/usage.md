# Create the resources using ARM template.

## Login into your Azure Subscription
`az login`

Select your azure subscription if required.

` az account set -s $subscription `

## Create Resource group
` az deployment sub create --location WestUS2  --template-file .\weatherResourceGroups.bicep --parameters name=myweather `

## Deploy search index.
Some bug in Azure Search Index is not allowing me to deploy to two regions at the same time so deploy both one by one.

` az deployment group create --resource-group myweather-wus2 --template-file .\searchIndex.bicep --parameters name=myweatherwus2 `

` az deployment group create --resource-group myweather-eus2 --template-file .\searchIndex.bicep --parameters name=myweathereus2 `

## Create Resources
` az deployment group create --resource-group myweather-wus2 --template-file .\weatherResources.bicep --parameters name=myweather `

## Upload the zip file
Upload the webjob zip.
