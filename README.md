![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# Expense Management System

A modernized expense management application built with ASP.NET 8.0, Azure SQL Database, and optional Azure OpenAI integration for intelligent chat features.

## Features

- **Dashboard**: View expense statistics and recent expenses at a glance
- **Expense Management**: Create, edit, submit, and delete expenses
- **Approval Workflow**: Managers can approve or reject submitted expenses
- **REST APIs**: Full API with Swagger documentation at `/swagger`
- **AI Chat Assistant**: Natural language interface to manage expenses (requires GenAI deployment)

## Deployment

### Prerequisites

1. Azure CLI installed and configured (`az login`)
2. Azure subscription with appropriate permissions
3. .NET 8.0 SDK (for local development)

### Basic Deployment (without AI Chat)

```bash
# Clone the repo
git clone <repo-url>
cd <repo-name>

# Login to Azure and set subscription
az login
az account set --subscription "<subscription-id>"

# Run the deployment script
./deploy.sh
```

### Full Deployment (with AI Chat Features)

```bash
# Run the full deployment script
./deploy-with-chat.sh
```

## Accessing the Application

After deployment, your application will be available at:
- **Web App**: `https://<app-name>.azurewebsites.net/Index`
- **API Docs**: `https://<app-name>.azurewebsites.net/swagger`

**Important**: Navigate to `/Index` to view the application (not the root URL)

## Local Development

1. Update `appsettings.Development.json` with your Azure SQL connection string
2. Login with Azure CLI: `az login`
3. Run the application:
   ```bash
   cd src/ExpenseManagement
   dotnet run
   ```

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed information about the Azure resources and their connections.

## Database

The application uses the Northwind database schema for expense management with the following key tables:
- **Expenses**: Main expense records
- **Users**: Employee and manager data
- **ExpenseCategories**: Travel, Meals, Supplies, etc.
- **ExpenseStatus**: Draft, Submitted, Approved, Rejected

All database operations use stored procedures - no direct table access from application code.
