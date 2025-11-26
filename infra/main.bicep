@description('Location for all resources')
param location string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Timestamp for unique naming')
param timestamp string = utcNow('ddHHmm')

@description('The Object ID of the Azure AD admin for SQL')
param adminObjectId string

@description('The login name of the Azure AD admin for SQL')
param adminLogin string

@description('Deploy GenAI resources')
param deployGenAI bool = false

// Resource Group is created by the deployment script

// App Service Module
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    baseName: baseName
    timestamp: timestamp
  }
}

// Azure SQL Module
module azureSql 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    baseName: baseName
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// GenAI Module (conditional)
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  params: {
    location: 'swedencentral'
    baseName: baseName
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceHostName string = appService.outputs.appServiceHostName
output managedIdentityId string = appService.outputs.managedIdentityId
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output managedIdentityName string = appService.outputs.managedIdentityName
output sqlServerName string = azureSql.outputs.sqlServerName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output databaseName string = azureSql.outputs.databaseName

// GenAI outputs (conditional with null-safe operators)
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genai.outputs.searchName : ''
