@description('Location for GenAI resources - must be swedencentral for GPT-4o')
param location string = 'swedencentral'

@description('Base name for resources')
param baseName string

@description('The principal ID of the managed identity for role assignments')
param managedIdentityPrincipalId string

// Azure OpenAI Service
resource openAI 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: toLower('aoai-${baseName}-${uniqueString(resourceGroup().id)}')
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: toLower('aoai-${baseName}-${uniqueString(resourceGroup().id)}')
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o Model Deployment
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

// Azure AI Search
resource aiSearch 'Microsoft.Search/searchServices@2023-11-01' = {
  name: toLower('search-${baseName}-${uniqueString(resourceGroup().id)}')
  location: 'uksouth'
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Role assignment for Managed Identity - Cognitive Services OpenAI User
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, 'Cognitive Services OpenAI User')
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role assignment for Managed Identity - Search Index Data Reader
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearch.id, managedIdentityPrincipalId, 'Search Index Data Reader')
  scope: aiSearch
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output openAIEndpoint string = openAI.properties.endpoint
output openAIName string = openAI.name
output openAIModelName string = gpt4oDeployment.name
output searchEndpoint string = 'https://${aiSearch.name}.search.windows.net'
output searchName string = aiSearch.name
