# Entity Framework + SQL Server Integration Guide

## Overview

I've successfully integrated Entity Framework Core 8.0 with SQL Server into your FormBuilder backend. All data is now persisted to a SQL Server database instead of being stored in memory.

## What Changed

### 1. **NuGet Packages Added** (Backend.csproj)
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
- Microsoft.EntityFrameworkCore.Tools (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)

### 2. **New Files Created**
- `backend/Data/FormBuilderDbContext.cs` - Entity Framework DbContext with all DbSets and relationships configured
- `backend/DATABASE_SETUP.md` - Detailed database setup instructions
- `setup-database.bat` - Windows batch script to automate migrations
- `setup-database.sh` - Linux/Mac bash script to automate migrations

### 3. **Updated Models** (with Primary Keys for EF)
- `backend/Models/FormSchema.cs` - Added Id properties to FormStep, FormField, FieldCondition, ValidationRule
- `backend/Models/BrandingSettings.cs` - Added Id property (default = 1 for singleton)

### 4. **Updated Services to Use EF**
- `backend/Services/FormSchemaService.cs` - Now uses FormBuilderDbContext
- `backend/Services/HelpArticleService.cs` - Now uses FormBuilderDbContext with full CRUD operations
- `backend/Services/BrandingSettingsService.cs` - Now uses FormBuilderDbContext instead of JSON file

### 5. **Enhanced HelpArticleController**
Added new endpoints:
- `PUT /api/helparticle/{id}` - Update existing article
- `DELETE /api/helparticle/{id}` - Delete article

### 6. **Configuration Updates**
- `backend/appsettings.json` - Added ConnectionStrings section with SQL Server connection
- `backend/Program.cs` - Registered DbContext with DI and changed services from Singleton to Scoped

## Database Connection String

The default connection string in `appsettings.json`:
```
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=FormBuilder;Trusted_Connection=true;TrustServerCertificate=true;"
}
```

Modify this if you have:
- A different SQL Server instance name
- A different database name
- Need to use SQL authentication

## Getting Started

### Option 1: Using the Setup Script (Recommended for Windows)

1. Open PowerShell or Command Prompt
2. Navigate to the FormBuilder root directory
3. Run:
   ```powershell
   .\setup-database.bat
   ```

This will:
- Create the initial migration
- Apply it to create the database
- Seed sample data

### Option 2: Manual Migration (If Script Doesn't Work)

1. Open terminal in the `backend` directory
2. Create initial migration:
   ```
   dotnet ef migrations add InitialCreate
   ```
3. Apply migration:
   ```
   dotnet ef database update
   ```

## Database Schema

Created tables:
- **FormSchemas** - Form definitions (Id, Title, Description)
- **FormSteps** - Form steps (Id, Title, FormSchemaId)
- **FormFields** - Form fields (Id, Name, Label, Type, FormStepId)
- **FieldConditions** - Visibility conditions (Id, FieldName, Operator, Value, FormFieldId)
- **ValidationRules** - Validation rules (Id, RuleType, Message, Value, FormFieldId)
- **HelpArticles** - Help center articles (Id, Title, Content, Category, CreatedAt)
- **BrandingSettings** - Branding config (Id, PrimaryColor, AccentColor, etc.)

## Key Features

✅ **Persistent Storage** - All data saved to SQL Server database
✅ **Relational Data** - Proper foreign key relationships between tables
✅ **Cascade Deletes** - Child records deleted when parent is deleted
✅ **Migration Support** - Easy to manage schema changes with EF migrations
✅ **Sample Data** - Mortgage application form auto-seeded on first run
✅ **Transaction Support** - Ready for ACID compliant operations

## Common Commands

### Create a new migration after model changes
```
cd backend
dotnet ef migrations add [MigrationName]
```

### Apply pending migrations
```
dotnet ef database update
```

### See all migrations
```
dotnet ef migrations list
```

### Revert to previous migration (careful!)
```
dotnet ef database update [PreviousMigrationName]
```

## Running the Application

After database setup:

1. Navigate to backend directory:
   ```
   cd backend
   ```

2. Run the application:
   ```
   dotnet run
   ```

3. The API will be available at:
   - http://localhost:5000 (HTTP)
   - https://localhost:5001 (HTTPS)
   - Swagger UI: https://localhost:5001/swagger/index.html

## Troubleshooting

### "The type initializer for 'Microsoft.Data.SqlClient.SqlConnection' threw an exception"
- Verify SQL Server instance is running
- Check connection string in appsettings.json
- Try using `localhost` instead of `.\\SQLEXPRESS`

### "Cannot open database 'FormBuilder' requested by the login"
- Run migrations again: `dotnet ef database update`
- Or manually create database in SQL Server Management Studio

### "A command is still running"
- Ensure previous command completed
- Try closing and reopening terminal

### EF Core Tools Not Found
```
dotnet tool install --global dotnet-ef
```

## Next Steps

1. Run database setup with `setup-database.bat`
2. Test the API with Swagger UI
3. Verify data persistence by:
   - Creating a new form
   - Restarting the application
   - Confirming the form still exists

## Additional Resources

- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [SQL Server Connection Strings](https://www.connectionstrings.com/sql-server/)
- [EF Core Migrations Guide](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
