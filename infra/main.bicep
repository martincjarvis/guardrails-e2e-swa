targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment, used to generate resource names')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
}

module web 'web.bicep' = {
  name: 'web'
  scope: rg
  params: {
    location: location
    resourceToken: resourceToken
  }
}

output AZURE_LOCATION string = location
output WEB_URI string = web.outputs.uri
