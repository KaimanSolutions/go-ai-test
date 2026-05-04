@echo off
REM Database Migration Setup Script for FormBuilder (Windows)

echo ================================================
echo FormBuilder - Entity Framework Setup
echo ================================================
echo.

REM Navigate to backend directory
cd backend

REM Check if .NET CLI is available
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET CLI not found. Please install .NET 10.0 SDK.
    exit /b 1
)

echo Step 1: Creating initial migration...
dotnet ef migrations add InitialCreate

if errorlevel 1 (
    echo ERROR: Failed to create migration
    exit /b 1
)

echo.
echo Step 2: Applying migration to database...
dotnet ef database update

if errorlevel 1 (
    echo ERROR: Failed to apply migration
    exit /b 1
)

echo.
echo ================================================
echo Database setup completed successfully!
echo ================================================
echo.
echo The database has been created with:
echo   - FormSchemas table
echo   - FormSteps table
echo   - FormFields table
echo   - FieldConditions table
echo   - ValidationRules table
echo   - HelpArticles table
echo   - BrandingSettings table
echo.
echo Sample data (Mortgage Loan Application form) has been added.
echo.
echo You can now run the application with: dotnet run
pause
