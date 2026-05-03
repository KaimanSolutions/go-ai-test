# Mortgage Originations Platform

A starter mortgage originations platform with:

- .NET 8 Web API backend
- Svelte frontend
- JWT authentication plus OpenID Connect SSO support
- Custom form builder with conditional field visibility and validation rules
- Rules engine for schema-driven submission validation

## Setup

### Backend

1. Open `backend`.
2. Run `dotnet restore`.
3. Run `dotnet run`.

### Frontend

1. Open `frontend`.
2. Run `npm install`.
3. Run `npm run dev`.

## Notes

- Replace `appsettings.json` values with a real OpenID Connect provider for SSO.
- Local login is seeded with `admin` / `Password123!`.
- The backend exposes `/api/formbuilder/schema/loan-application` and `/api/formbuilder/validate`.
