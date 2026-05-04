#!/bin/bash
# Database Migration Setup Script for FormBuilder

echo "================================================"
echo "FormBuilder - Entity Framework Setup"
echo "================================================"
echo ""

# Navigate to backend directory
cd backend

# Check if .NET CLI is available
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET CLI not found. Please install .NET 10.0 SDK."
    exit 1
fi

echo "Step 1: Creating initial migration..."
dotnet ef migrations add InitialCreate

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to create migration"
    exit 1
fi

echo ""
echo "Step 2: Applying migration to database..."
dotnet ef database update

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to apply migration"
    exit 1
fi

echo ""
echo "================================================"
echo "✓ Database setup completed successfully!"
echo "================================================"
echo ""
echo "The database has been created with:"
echo "  - FormSchemas table"
echo "  - FormSteps table"
echo "  - FormFields table"
echo "  - FieldConditions table"
echo "  - ValidationRules table"
echo "  - HelpArticles table"
echo "  - BrandingSettings table"
echo ""
echo "Sample data (Mortgage Loan Application form) has been added."
echo ""
echo "You can now run the application with: dotnet run"
