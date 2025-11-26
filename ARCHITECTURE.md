# Azure Architecture Diagram

This document describes the Azure services deployed by this repository and how they connect.

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            Azure Resource Group                                  │
│                          (rg-expensemgmt-demo)                                  │
│                                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                        User-Assigned Managed Identity                    │   │
│  │                     (mid-appmodassist-[timestamp])                       │   │
│  │                                                                          │   │
│  │  Used by App Service to authenticate to:                                 │   │
│  │  • Azure SQL Database                                                    │   │
│  │  • Azure OpenAI (when deployed)                                          │   │
│  │  • Azure AI Search (when deployed)                                       │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                    │                                            │
│                                    ▼                                            │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                      App Service (Standard S1)                           │   │
│  │                   (app-expensemgmt-[unique])                             │   │
│  │                                                                          │   │
│  │  ASP.NET 8.0 Razor Pages Application                                     │   │
│  │  • Dashboard with expense statistics                                     │   │
│  │  • Expense management (CRUD operations)                                  │   │
│  │  • Expense approval workflow                                             │   │
│  │  • REST APIs with Swagger documentation                                  │   │
│  │  • AI Chat interface (when GenAI deployed)                               │   │
│  │                                                                          │   │
│  │  Endpoints:                                                              │   │
│  │  • /Index - Dashboard                                                    │   │
│  │  • /Expenses - Expense list and management                               │   │
│  │  • /Approvals - Approval workflow                                        │   │
│  │  • /swagger - API documentation                                          │   │
│  │  • /api/expenses - REST API                                              │   │
│  │  • /api/chat - Chat API                                                  │   │
│  └───────────────────────────────┬─────────────────────────────────────────┘   │
│                                  │                                              │
│              ┌───────────────────┼───────────────────┐                          │
│              │                   │                   │                          │
│              ▼                   ▼                   ▼                          │
│  ┌───────────────────┐ ┌─────────────────┐ ┌─────────────────────┐             │
│  │   Azure SQL       │ │  Azure OpenAI   │ │   Azure AI Search   │             │
│  │   Database        │ │  (Optional)     │ │   (Optional)        │             │
│  │                   │ │                 │ │                     │             │
│  │ Server:           │ │ Location:       │ │ SKU: Basic          │             │
│  │ sql-expensemgmt-  │ │ Sweden Central  │ │ Location: UK South  │             │
│  │ [unique]          │ │                 │ │                     │             │
│  │                   │ │ Model:          │ │ Used for:           │             │
│  │ Database:         │ │ GPT-4o          │ │ RAG pattern         │             │
│  │ Northwind         │ │ Capacity: 8     │ │ (future)            │             │
│  │                   │ │                 │ │                     │             │
│  │ SKU: Basic        │ │ SKU: S0         │ │                     │             │
│  │                   │ │                 │ │                     │             │
│  │ Auth: Entra ID    │ │ Auth: Managed   │ │ Auth: Managed       │             │
│  │ Only              │ │ Identity        │ │ Identity            │             │
│  └───────────────────┘ └─────────────────┘ └─────────────────────┘             │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘

                                    │
                                    │ HTTPS
                                    ▼
                          ┌─────────────────┐
                          │     Users       │
                          │                 │
                          │ Web Browser     │
                          │ or API Client   │
                          └─────────────────┘
```

## Deployment Options

### Basic Deployment (deploy.sh)
Deploys:
- Resource Group
- User-Assigned Managed Identity
- App Service Plan (S1)
- App Service
- Azure SQL Server (Entra ID only auth)
- Azure SQL Database (Northwind)

### Full Deployment (deploy-with-chat.sh)
Deploys everything in basic deployment plus:
- Azure OpenAI Service (Sweden Central)
- GPT-4o Model Deployment
- Azure AI Search Service

## Security

- **No SQL Authentication**: Azure SQL uses Entra ID (Azure AD) only authentication
- **Managed Identity**: All service-to-service communication uses the User-Assigned Managed Identity
- **Role-Based Access**: The Managed Identity has specific roles:
  - `db_datareader` and `db_datawriter` on SQL Database
  - `Cognitive Services OpenAI User` on Azure OpenAI
  - `Search Index Data Reader` on AI Search

## Data Flow

1. User accesses the web application via HTTPS
2. App Service authenticates to Azure SQL using Managed Identity
3. All database operations go through stored procedures (no direct table access)
4. When GenAI is deployed:
   - Chat requests are sent to Azure OpenAI
   - AI uses function calling to interact with the database through the API
   - Responses are formatted and returned to the user

## Local Development

For local development, update `appsettings.Development.json`:
- Use `Authentication=Active Directory Default` in connection string
- Run `az login` to authenticate with your Azure AD account
