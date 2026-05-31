# SmartSchool

A full-stack school management system for Sri Lanka, built with a GraphQL API backend and an Angular SPA frontend.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Repository Structure](#repository-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Database](#database)
  - [Backend](#backend)
  - [Frontend](#frontend)
- [Configuration](#configuration)
- [Domain Features](#domain-features)
- [API](#api)
- [Project Layout — Backend](#project-layout--backend)
- [Project Layout — Frontend](#project-layout--frontend)
- [Development Notes](#development-notes)

---

## Overview

SmartSchool manages the full lifecycle of school operations in Sri Lanka:

- **Student enrollment** — requests, approvals, class assignments
- **Teacher & principal enrollment** — per-school, per-academic-year
- **Hierarchical geography** — Province → District → Zone → Division → School
- **Academic years & grading** — Grades 1–13, section-based classes
- **User accounts** — JWT-authenticated, role-linked to student/teacher/principal records
- **Real-time updates** — GraphQL subscriptions push enrollment changes to all open clients

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend runtime | .NET 8 (ASP.NET Core) |
| GraphQL server | HotChocolate 13 |
| ORM | Entity Framework Core 8 (Pomelo MySQL driver) |
| Database | MariaDB 11.4 |
| Authentication | JWT Bearer |
| Object mapping | AutoMapper |
| Notifications | MailKit (email) · SMS via HTTP API |
| Frontend framework | Angular 17 (standalone components, signals) |
| GraphQL client | Apollo Angular 7 (`@apollo/client` 3) |
| UI components | Angular Material 17 · Bootstrap 5 · Bootstrap Icons |
| Forms | Reactive Forms · ngx-formly |
| Data grid | AG Grid 31 |
| Toasts | ngx-toastr |
| i18n | ngx-translate |
| Containerisation | Docker · Docker Compose |

---

## Repository Structure

```
smartschool/
├── smartschool-svc/          # .NET 8 backend
│   ├── SmartSchool.Api/      # ASP.NET Core host (Program.cs, JWT, CORS, Kestrel)
│   ├── SmartSchool.Graphql/  # HotChocolate queries, mutations, subscriptions, models
│   ├── SmartSchool.Schema/   # EF Core entities, DbContext, migrations, seed data
│   ├── SmartSchool.Service/  # Domain services (auth, email, SMS notifications)
│   ├── SmartSchool.Utility/  # Shared helpers (password hashing, validation)
│   ├── SmartSchool.Resources/# Resource descriptor metadata layer (new-arch branch)
│   └── SmartSchool.Tests/    # xUnit tests (new-arch branch)
├── smartschool-gui/          # Angular 17 SPA
│   └── src/app/
│       ├── auth/             # Login, register, verify, JWT interceptor, guards
│       ├── schools/          # Schools, classes, enrollment requests & enrollments
│       ├── students/         # Student list and detail pages
│       ├── teachers/         # Teacher list and detail pages
│       ├── principals/       # Principal list and detail pages
│       ├── users/            # User accounts and linked records
│       ├── dashboard/        # Home page
│       └── shared/           # GraphQL service, queries, mutations, pipes, components
├── dns-server.js             # Local DNS server for dev (resolves smartschool.app)
├── docker-compose.yml        # MariaDB + app stack
└── docker-compose.debug.yml  # VS Code debug override
```

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 8.x | `dotnet --version` |
| Node.js | 18+ or 20+ | Use nvm: `nvm install 20 && nvm use 20` |
| Docker Desktop | any | For MariaDB container |
| dotnet-ef | 8.x | `dotnet tool install -g dotnet-ef --version 8.*` |

---

## Getting Started

### Database

Start MariaDB in Docker:

```powershell
docker compose up mariadb -d
```

Apply EF Core migrations (creates all tables + seed data):

```powershell
cd smartschool-svc
dotnet ef database update --project SmartSchool.Schema --startup-project SmartSchool.Api
```

### Backend

```powershell
cd smartschool-svc/SmartSchool.Api
dotnet run --launch-profile Kestrel
# GraphQL playground: http://localhost:5000/graphql
# Health check:       http://localhost:5000/healthz
```

### Frontend

```powershell
cd smartschool-gui
npm install
npm start
# App: http://localhost:4200
```

---

## Configuration

### `appsettings.Development.json` (gitignored in production, committed for dev)

```json
{
  "ConnectionStrings": {
    "MariaDbConnection": "server=localhost;port=3306;database=smartschool;user=root;password=root"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "http://localhost:5000" }
    }
  }
}
```

### `appsettings.json` (base — do not add machine-specific cert paths here)

Key sections:

| Key | Purpose |
|---|---|
| `AuthSettings.Secret` | JWT signing secret |
| `SmtpSettings` | Outbound email (MailKit) |
| `SmsSettings` | SMS API credentials |
| `ConnectionStrings.MariaDbConnection` | Database connection string |

---

## Domain Features

### Geography (Sri Lanka admin hierarchy)

```
Province → District → Zone → Division → School
```

9 provinces, 25 districts, zones, and divisions are seeded at startup.

### Academic structure

- **Academic years** — year-keyed (e.g. 2026), with start/end dates
- **Classes** — per school, per grade (1–13), per section (A/B/C…), per medium language
- **Grades** — `Grade1` through `Grade13`

### Enrollment lifecycle

```
SchoolStudentEnrollmentRequest  →  (Approved by principal)
  → SchoolStudentEnrollment     →  ClassStudentEnrollment
```

Status flow: `Pending → Approved / Rejected / Cancelled`

Same pattern for teachers: `SchoolTeacherEnrollmentRequest → SchoolTeacherEnrollment → ClassTeacherEnrollment`

### Persons & Users

- `Person` — core identity record (name, NIC, DoB, contact details, photo)
- `Student` / `Teacher` / `Principal` — role records that reference a `Person`
- `User` — login account that references a `Person`, can be linked to any role

### Seeded accounts

| User ID | Name | Password | Role |
|---|---|---|---|
| 1 | System Admin | `1111` | Admin |
| 2 | Nifraz Navahz | `2222` | Student |
| 3 | Ayesha Rauf | `3333` | Teacher |
| 4 | Mohamad Navahz | `4444` | Teacher |
| 5 | Nisry Ahamed | `5555` | Principal |

---

## API

The backend exposes a single GraphQL endpoint.

**Endpoint:** `http://localhost:5000/graphql`  
**WebSocket (subscriptions):** `ws://localhost:5000/graphql`

### Key queries

| Query | Description |
|---|---|
| `users` | Paginated, filterable user list |
| `persons` | Person records with age calculation |
| `students` / `teachers` / `principals` | Role-specific lists |
| `schools` | Filterable school list with classes and enrollments |
| `academicYears` | Available academic years |
| `divisions` | Geography lookup |
| `school(id)` | Full school detail including enrollment history |
| `schoolReport(input)` | Aggregated school statistics |

### Key mutations

| Mutation | Description |
|---|---|
| `login` / `register` / `verify` | Auth via REST (`/api/auth/*`) |
| `createSchoolStudentEnrollmentRequest` | Student applies to a school |
| `updateSchoolStudentEnrollmentRequestStatus` | Principal approves/rejects |
| `createSchoolStudentEnrollment` | Enroll an approved student |

### Subscriptions

| Subscription | Description |
|---|---|
| `schoolStudentEnrollmentProcessed` | Fires when an enrollment request changes status |
| `schoolStudentEnrollmentRequestProcessed` | Fires when a request is processed |

---

## Project Layout — Backend

```
SmartSchool.Api/
  Program.cs              — DI wiring: EF, HotChocolate, JWT, CORS, AutoMapper
  Controllers/AuthController.cs — REST endpoints: /api/auth/{login,register,verify}
  appsettings.json        — base config
  appsettings.Development.json — dev overrides (DB connection, HTTP-only Kestrel)

SmartSchool.Graphql/
  Queries/
    SchoolsQuery.cs       — schools, academicYears, divisions (offset-paged, filtered, sorted)
    UsersQuery.cs         — users, user(id)
    StudentsQuery.cs      — students, student(id)
    TeachersQuery.cs      — teachers, teacher(id)
    PersonsQuery.cs       — persons
  Mutations/
    SchoolMutation.cs     — enrollment request + enrollment CRUD
    StudentMutation.cs    — student record management
    TeacherMutation.cs    — teacher record management
    PersonMutation.cs     — person CRUD
    UserMutation.cs       — user management
  Subscriptions/
    SchoolSubscription.cs — real-time enrollment events
  Models/                 — AutoMapper DTOs (read models)
  Inputs/                 — Input types for mutations

SmartSchool.Schema/
  Entities/               — EF Core entity classes
    AbstractRecord.cs     — base: audit fields + soft-delete (DeletedTime)
    Person.cs, Student.cs, Teacher.cs, Principal.cs, User.cs
    School.cs, Class.cs, AcademicYear.cs
    SchoolStudentEnrollment*.cs, SchoolTeacherEnrollment*.cs
    Province.cs, District.cs, Zone.cs, Division.cs
    Locale.cs, Rbac.cs    — i18n + RBAC tables (used by new-arch branch)
  AppDbContext.cs          — pooled DbContext, soft-delete filter, audit SaveChanges
  ModelBuilderExtensions.cs — seed data (provinces, districts, schools, users…)
  Migrations/             — EF Core migrations (Pomelo/MariaDB)
  Enums/                  — Grade, Sex, EnrollmentStatus, RequestStatus, SchoolType…

SmartSchool.Service/
  Services/AuthService.cs — password hashing (bcrypt), JWT generation
  Services/EmailService.cs — MailKit SMTP
  Services/NotificationService.cs — email + SMS dispatch
```

---

## Project Layout — Frontend

```
src/app/
  app.config.ts           — Apollo (HTTP + WS split link), provideHttpClient,
                            FormlyModule, ToastrModule, ngx-translate
  app.routes.ts           — route definitions with lazy loading and auth guards
  auth/
    auth.service.ts       — login/register/verify, JWT session (localStorage)
    jwt.interceptor.ts    — attaches Bearer token to every HTTP request
    guards/               — auth.guard (redirect to login), account.guard (redirect if authed)
  schools/
    school-details/       — school info + create enrollment request modal
    school-student-enrollment-requests/ — list + detail with approve/reject/cancel
    school-student-enrollments/         — enrollment list + detail
    classes/              — class detail with student roster
  students/ teachers/ principals/ users/
    *-details/            — detail page with edit form and enrollment history
    *.component.ts        — AG Grid list with server-side filtering
  shared/
    services/graphql.service.ts — Apollo watchQuery/mutate/subscribe wrappers
    queries.ts            — all gql query documents
    mutations.ts          — all gql mutation documents
    subscriptions.ts      — all gql subscription documents
    components/           — RecordComponent, GraphqlRecordFormComponent, autocomplete
    pipes/                — TitleCaseWithSpace
```

---

## Development Notes

**Adding a new academic year** — insert directly into the DB or update the seed in `ModelBuilderExtensions.cs` and create a new migration:
```powershell
dotnet ef migrations add AddAcademicYear20XX --project SmartSchool.Schema --startup-project SmartSchool.Api
dotnet ef database update --project SmartSchool.Schema --startup-project SmartSchool.Api
```

**Accessing from a phone on the same WiFi** — run the local DNS server (requires elevated terminal), then set the phone's DNS to `192.168.1.20`:
```powershell
node dns-server.js   # resolves smartschool.app → 192.168.1.20
npm start            # serves on 0.0.0.0:4200, allows smartschool.app host
```
Browse to `http://smartschool.app:4200` from any device on the network.

**Two git identities** — this repo uses the personal account. The local config is set to `nifraz / nifraz@live.com`; global config remains as the work identity.

**CORS** — the backend currently allows only `http://localhost:4200`. Update `Program.cs` `WithOrigins(...)` when deploying or adding additional origins.

**Branch `new-arch`** — contains a resource-engine refactor (backend-driven UI, generic GraphQL CRUD, EF migrations for i18n + RBAC). Not yet merged to `main`.
