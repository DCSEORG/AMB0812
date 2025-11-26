#!/bin/bash

# ============================================================================
# Expense Management System - Deployment Script
# ============================================================================
# This script deploys the App Service, Azure SQL Database, and application code
# without GenAI resources. For full deployment including Chat UI, use:
# ./deploy-with-chat.sh
# ============================================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}  Expense Management System Deployment${NC}"
echo -e "${GREEN}============================================${NC}"

# Configuration - Update these values as needed
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
BASE_NAME="expensemgmt"

# Get current user's Object ID and UPN for SQL admin
echo -e "${YELLOW}Getting current user information...${NC}"
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)
ADMIN_LOGIN=$(az ad signed-in-user show --query userPrincipalName -o tsv)

echo "Admin Object ID: $ADMIN_OBJECT_ID"
echo "Admin Login: $ADMIN_LOGIN"

# Create resource group if it doesn't exist
echo -e "${YELLOW}Creating resource group...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Deploy infrastructure
echo -e "${YELLOW}Deploying infrastructure (App Service, SQL Database)...${NC}"
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group $RESOURCE_GROUP \
    --template-file infra/main.bicep \
    --parameters baseName=$BASE_NAME \
    --parameters adminObjectId=$ADMIN_OBJECT_ID \
    --parameters adminLogin="$ADMIN_LOGIN" \
    --parameters deployGenAI=false \
    --query "properties.outputs" \
    -o json)

# Extract output values
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.databaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')

echo -e "${GREEN}Infrastructure deployed successfully!${NC}"
echo "App Service: $APP_SERVICE_NAME"
echo "SQL Server: $SQL_SERVER_FQDN"
echo "Database: $DATABASE_NAME"
echo "Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo "Managed Identity Name: $MANAGED_IDENTITY_NAME"

# Configure App Service settings
echo -e "${YELLOW}Configuring App Service settings...${NC}"
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
    --output none

az webapp config connection-string set \
    --name $APP_SERVICE_NAME \
    --resource-group $RESOURCE_GROUP \
    --connection-string-type SQLAzure \
    --settings "DefaultConnection=$CONNECTION_STRING" \
    --output none

# Wait for SQL Server to be fully ready
echo -e "${YELLOW}Waiting 30 seconds for SQL Server to be fully ready...${NC}"
sleep 30

# Add local IP to firewall
echo -e "${YELLOW}Adding local IP to SQL Server firewall...${NC}"
LOCAL_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "LocalDevelopment" \
    --start-ip-address $LOCAL_IP \
    --end-ip-address $LOCAL_IP \
    --output none 2>/dev/null || true

# Install Python dependencies
echo -e "${YELLOW}Installing Python dependencies...${NC}"
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with correct values
echo -e "${YELLOW}Updating Python scripts with deployment values...${NC}"
sed -i.bak "s/sql-expensemgmt.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Import database schema
echo -e "${YELLOW}Importing database schema...${NC}"
python3 run-sql.py

# Configure database roles for managed identity
echo -e "${YELLOW}Configuring database roles for managed identity...${NC}"
python3 run-sql-dbrole.py

# Deploy stored procedures
echo -e "${YELLOW}Deploying stored procedures...${NC}"
python3 run-sql-stored-procs.py

# Build and package the application
echo -e "${YELLOW}Building application...${NC}"
cd src/ExpenseManagement
dotnet publish -c Release -o ./publish

# Create zip file with correct structure (files at root, not in subdirectory)
echo -e "${YELLOW}Creating deployment package...${NC}"
cd publish
zip -r ../../../app.zip .
cd ../../..

# Deploy application code
echo -e "${YELLOW}Deploying application code...${NC}"
az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip

echo -e "${GREEN}============================================${NC}"
echo -e "${GREEN}  Deployment Complete!${NC}"
echo -e "${GREEN}============================================${NC}"
echo ""
echo -e "App URL: ${GREEN}https://${APP_SERVICE_NAME}.azurewebsites.net/Index${NC}"
echo ""
echo -e "${YELLOW}Note: Navigate to /Index to view the application${NC}"
echo -e "${YELLOW}API Documentation: https://${APP_SERVICE_NAME}.azurewebsites.net/swagger${NC}"
echo ""
echo -e "${YELLOW}To enable AI Chat features, run:${NC}"
echo -e "${GREEN}./deploy-with-chat.sh${NC}"
