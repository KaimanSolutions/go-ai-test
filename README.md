# Mortgage Originations Platform

A full-stack mortgage originations platform built with a .NET 10 Web API backend and Svelte 5 frontend. It provides a complete workflow for creating and managing mortgage applications, from configurable form schemas through to document collection, policy rules and communication templates.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10 Web API, Entity Framework Core 8, SQL Server |
| Frontend | Svelte 5 (runes), Vite, Tailwind CSS |
| Auth | JWT Bearer + OpenID Connect SSO |
| Email | Mjml.Net v4 (server-side MJML → HTML compilation) |

---

## Features

### Form Builder
- Drag-and-drop multi-step form schema editor
- Field types: text, email, phone, number, currency, textarea, date, select, radio, checkbox, address, repeater, info/heading
- Per-field conditional visibility rules and validation rules
- Step-level conditions (show/hide entire steps)
- Info fields with four style variants: heading, sub-heading, paragraph, warning banner
- Live preview panel while editing
- Schema versioning — schemas are soft-archived rather than deleted, preserving references from existing applications

### Applications
- Create applications against any active form schema
- Multi-step form filling with auto-save on each step navigation
- Field validation (required, min/max, regex, length) and custom error messages
- Submitted application view with read-only form summary
- Search and filter by reference number, form title, and status
- Date values formatted as `dd/mm/yyyy` throughout

### Workflows
- Define named workflows with ordered stages (initial → intermediate → final)
- Configurable stage transitions
- Assign a workflow to a form schema so applications are automatically enrolled
- Stage tracker displayed on submitted applications
- Admin can manually advance the current stage

### Policy Rules Engine
- Create business rules linked to a specific form schema
- Expression-based conditions using form field values, arithmetic, and aggregation functions (`SUM`, `AVG`, `COUNT`) over repeater fields
- AND/OR condition groups
- `Decline` or `Refer` decision types
- Client-visible and broker-visible descriptions per rule
- Rules are automatically evaluated when an application is submitted and persisted as outcome history
- Outcome history shown on the submitted application view with pass/fail badges

### Checklist
- Admin-configurable document and information request templates
- Each item is linked to a form schema and can have conditions that match form field values
- Client-visible and broker-visible flags
- Generated automatically against a submitted application, with conditions evaluated against the application's form data
- **Status lifecycle:** Outstanding → Pending Review → Approved | More Info Needed | Rejected
- Information items: free-text response area
- Document items: file upload, stored on disk and served via authenticated download endpoint
- Admin can approve, request more info, or reject each item
- Comments thread on each item — any authenticated user can post; author name and timestamp recorded

### Templates
- Manage email, SMS, and document templates from a single admin interface
- **Email:** written in MJML with a live server-side preview (compiled to HTML by Mjml.Net)
- **SMS:** plain text with character and message count indicator
- **Document:** HTML with a rendered preview
- `{{variable}}` placeholder syntax — available variables listed in a sidebar (applicant name, reference, form title, submitted date, broker name, company, site URL)
- Active/inactive flag per template

### Companies & Brokers
- Company registry with FCA number, status, type (Network, DA, AR), trading names and addresses
- Parent/child company hierarchy (network → member firms)
- Bank details management
- Companies House integration for registered address lookup

### Users & Auth
- Local admin account (configured via `appsettings.json`)
- Database-backed broker/client user accounts with role-based access
- User lockout
- OpenID Connect SSO support
- JWT Bearer authentication

### Integrations
- API request log with per-integration filtering
- EPC (Energy Performance Certificate) lookup
- Companies House search
- Integration settings stored per-key in the database

### Help Centre
- Markdown-based help articles with category and portal visibility controls (admin / broker / client)

### Branding & Settings
- Configurable colour palette and heading/body font family
- Live theme preview

---

## Project Structure

```
FormBuilder/
├── backend/
│   ├── Features/
│   │   ├── Applications/     Application model, service, controller
│   │   ├── Auth/             JWT service, external auth (OIDC)
│   │   ├── Checklist/        Checklist templates + application items
│   │   ├── Companies/        Company registry, FCA lookup
│   │   ├── Forms/            Form schema, form builder, rule engine
│   │   ├── Help/             Help articles
│   │   ├── Integrations/     EPC, Companies House, API log
│   │   ├── Profile/          User profile settings
│   │   ├── Rules/            Business rules engine
│   │   ├── Settings/         Branding settings
│   │   ├── Templates/        Email/SMS/document templates
│   │   ├── Users/            User accounts
│   │   └── Workflows/        Workflow stages and transitions
│   ├── Data/
│   │   └── FormBuilderDbContext.cs
│   ├── Migrations/
│   ├── Program.cs
│   └── Backend.csproj
│
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   │   ├── Applications.svelte
│   │   │   ├── Auth.svelte
│   │   │   ├── BrokerPortal.svelte
│   │   │   ├── Checklist.svelte
│   │   │   ├── ClientPortal.svelte
│   │   │   ├── Companies.svelte
│   │   │   ├── FormBuilder.svelte
│   │   │   ├── FormRenderer.svelte
│   │   │   ├── FormRunner.svelte
│   │   │   ├── HelpCentre.svelte
│   │   │   ├── Integrations.svelte
│   │   │   ├── Profile.svelte
│   │   │   ├── Rules.svelte
│   │   │   ├── Settings.svelte
│   │   │   ├── Templates.svelte
│   │   │   ├── Users.svelte
│   │   │   └── Workflows.svelte
│   │   ├── lib/
│   │   │   └── auth.js       All API client functions
│   │   └── App.svelte
│   └── package.json
│
├── backend.Tests/
└── README.md
```

---

## Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- SQL Server (local or remote)

### 1. Database

Create a SQL Server database and update the connection string in `backend/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=FormBuilder;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Then apply all migrations from the `backend/` directory:

```bash
dotnet ef database update
```

### 2. Backend

```bash
cd backend
dotnet restore
dotnet run
```

The API starts on `https://localhost:5001` (or the port shown in the console).

### 3. Frontend

```bash
cd frontend
npm install
npm run dev
```

The dev server starts on `http://localhost:5173` and proxies API requests to the backend.

---

## Default Credentials

| Account | Username | Password |
|---|---|---|
| Admin | `admin` | `Password123!` |

The admin credentials can be changed in `appsettings.json` under the `AdminAccount` section.

---

## Configuration

### `backend/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "AdminAccount": {
    "Username": "admin",
    "Password": "Password123!",
    "Email": "admin@example.com"
  },
  "Jwt": {
    "Key": "your-secret-key-at-least-32-chars",
    "Issuer": "FormBuilder",
    "Audience": "FormBuilder"
  },
  "OpenIdConnect": {
    "Authority": "",
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

Leave `OpenIdConnect` empty to disable SSO. When configured, a **Sign in with SSO** button appears on the login page.

---

## Key API Endpoints

All endpoints require a `Bearer` JWT token unless marked as public.

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/auth/login` | Obtain a JWT (public) |
| `GET` | `/api/formbuilder/schemas` | List active form schemas (public) |
| `GET` | `/api/formbuilder/schema/{id}` | Get a full form schema (public) |
| `POST` | `/api/formbuilder/schema` | Create or update a schema |
| `DELETE` | `/api/formbuilder/schema/{id}` | Archive a schema |
| `POST` | `/api/formbuilder/schema/{id}/restore` | Restore an archived schema |
| `GET` | `/api/applications` | List applications |
| `POST` | `/api/applications` | Create an application |
| `PUT` | `/api/applications/{id}` | Update form data / stage |
| `GET` | `/api/rules` | List business rules |
| `POST` | `/api/rules/evaluate` | Evaluate rules against form data |
| `GET` | `/api/checklist` | List checklist templates |
| `GET` | `/api/checklist/application/{id}` | Get checklist items for an application |
| `POST` | `/api/checklist/application/{id}/generate` | Generate checklist items from templates |
| `PUT` | `/api/checklist/application/item/{id}/respond` | Submit an information response |
| `POST` | `/api/checklist/application/item/{id}/upload` | Upload a document |
| `GET` | `/api/checklist/application/item/{id}/download` | Download an uploaded document |
| `PUT` | `/api/checklist/application/item/{id}/status` | Update item status (admin only) |
| `POST` | `/api/checklist/application/item/{id}/comment` | Add a comment to a checklist item |
| `GET` | `/api/templates` | List templates (admin only) |
| `POST` | `/api/templates/compile-mjml` | Compile MJML to HTML (admin only) |
| `GET` | `/api/workflows` | List workflows |
| `GET` | `/api/company` | List companies |

---

## Portals

The platform serves three distinct portal views from a single application:

- **Admin portal** — full access to all features: form builder, applications, workflows, rules, checklist, templates, companies, users, integrations, settings
- **Broker portal** — access to applications and checklist items marked as broker-visible
- **Client portal** — access to their own applications and checklist items marked as client-visible

---

## Running Tests

```bash
cd backend.Tests
dotnet test
```
