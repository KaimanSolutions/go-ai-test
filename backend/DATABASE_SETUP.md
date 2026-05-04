# Entity Framework Database Setup Guide

This guide will help you set up the SQL Server database for the FormBuilder application using Entity Framework Core.

## Prerequisites

- SQL Server (LocalDB or SQL Server Express)
- .NET 10.0 SDK or later

## Setup Steps

### 1. Update Connection String (if needed)

The connection string is configured in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=FormBuilder;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

Modify this connection string if:
- Your SQL Server instance name is different
- You're using a different database name
- You need to use SQL authentication instead of Windows authentication

### 2. Create Initial Migration

From the `backend` directory, run:

```powershell
dotnet ef migrations add InitialCreate
```

This creates a migration file that defines the database schema.

### 3. Apply Migration to Database

Run the following command to create the database and apply the migration:

```powershell
dotnet ef database update
```

This will:
- Create the FormBuilder database (if it doesn't exist)
- Create all required tables (FormSchemas, FormSteps, FormFields, HelpArticles, BrandingSettings, etc.)
- Seed sample data (one mortgage application form)

### 4. Verify Database Creation

You can verify the database was created by:
- Opening SQL Server Management Studio (SSMS)
- Connecting to your SQL Server instance
- Looking for the "FormBuilder" database
- Checking the tables under Databases > FormBuilder > Tables

## Common Commands

### Create a new migration after model changes

```powershell
dotnet ef migrations add [MigrationName]
```

Example: `dotnet ef migrations add AddNewFeature`

### Apply pending migrations

```powershell
dotnet ef database update
```

### Drop the database (careful!)

```powershell
dotnet ef database drop
```

### See migration history

```powershell
dotnet ef migrations list
```

## Troubleshooting

### "No database provider has been configured for this DbContext"

Make sure the connection string exists in `appsettings.json` and the DbContext is registered in `Program.cs`.

### Connection refused / Cannot connect to SQL Server

- Verify SQL Server is running
- Check the server name in the connection string
- Try using `localhost` instead of `.\\SQLEXPRESS` if using named pipes

### "The type initializer for 'Microsoft.Data.SqlClient.SqlConnection' threw an exception"

This usually indicates a connection string issue. Check the format and ensure the server instance exists.

## Database Schema

The database includes the following tables:

- **FormSchemas**: Main form definitions
- **FormSteps**: Form steps within schemas
- **FormFields**: Individual fields within steps
- **FieldConditions**: Conditional field visibility rules
- **ValidationRules**: Field validation rules
- **HelpArticles**: Help center articles with categories
- **BrandingSettings**: Application branding configuration

## Next Steps

1. Start the backend: `dotnet run`
2. The API will initialize default data on first run
3. Access Swagger at: http://localhost:5000/swagger/index.html
