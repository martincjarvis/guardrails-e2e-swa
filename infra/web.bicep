@description('Primary location for all resources')
param location string

@description('Token used to build unique resource names')
param resourceToken string

resource swa 'Microsoft.Web/staticSites@2023-12-01' = {
  name: 'swa-${resourceToken}'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    provider: 'Custom'
  }
}

output uri string = 'https://${swa.properties.defaultHostname}'
