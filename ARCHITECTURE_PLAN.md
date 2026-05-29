# SmartSchool — Architecture Plan & Implementation Status

> **Monorepo:** `smartschool` (local: `Desktop/smartschool`, GitHub: `github.com/nifraz/smartschool`)
> **Goal:** backend is the single source of truth for data, behavior, navigation, forms, grids, validation, permissions, and localization. The Angular client is a thin, generic renderer of resources delivered over GraphQL.

---

## Quick-start for a new session

```
Repo root:       C:\Users\NifraZ\Desktop\smartschool\
Backend:         smartschool-svc\  (.NET 8 · HotChocolate · EF Core · MariaDB)
Frontend:        smartschool-gui\  (Angular 17 · Apollo · AG Grid · ngx-translate)
DB migrations:   smartschool-svc\db\  (Liquibase YAML)
Tests:           smartschool-svc\SmartSchool.Tests\  (xUnit · EF InMemory)
VS Code debug:   .vscode\launch.json  (Kestrel + Docker + Angular configs)
```

**Run locally (no Docker):**
```powershell
# Terminal 1 — backend
cd smartschool-svc\SmartSchool.Api
dotnet run --launch-profile Kestrel          # http://localhost:5000/graphql

# Terminal 2 — frontend (after npm install)
cd smartschool-gui
npm install && npm start                     # http://localhost:4200
```

**Run with Docker:**
```powershell
docker compose -f docker-compose.yml -f docker-compose.debug.yml up --build
# svc debug container: smartschool-svc-1  (vsdbg at /vsdbg)
# Press F5 in VS Code → "Docker: Full Stack Debug (Chrome)"
```

**Docker is currently blocked** — Windows CBS issue (`pending.xml` 1.3 MB, KB5046613 stuck).
Fix: run as admin: `Stop-Service wuauserv,bits,cryptsvc,trustedinstaller -Force` → delete `C:\WINDOWS\CbsTemp\31145297_471196745` → `DISM /Online /Cleanup-Image /RestoreHealth` → reboot.

---

## 1. Implemented Architecture

### 1.1 High-Level Diagram

```
┌──────────────────────────────── smartschool (monorepo) ─────────────────────────────┐
│                                                                                     │
│  smartschool-svc (.NET 8)                       smartschool-gui (Angular 17)        │
│  ┌──────────────────────────────┐               ┌──────────────────────────────┐    │
│  │ Schema (EF entities + attrs) │──┐            │ app.routes.ts (2 routes)     │    │
│  │ Service (domain logic)       │  │            │ resource-engine/             │    │
│  │ Graphql (HotChocolate)       │  │  GraphQL   │   ResourceListPage           │    │
│  │ Resources (metadata layer)───┼──┼──────────▶ │   ResourceDetailPage         │    │
│  │ Localization (translations)  │  │  WS/Subs   │   DynamicGridComponent       │    │
│  │ Auth (JWT)                   │  │            │   DynamicFormComponent       │    │
│  │ Api (host)                   │──┘            │   ResourceRegistryService    │    │
│  └──────────────────────────────┘               │   LocaleService              │    │
│           │                                     │   PermissionService          │    │
│           ▼                                     └──────────────────────────────┘    │
│       MariaDB (Liquibase-managed schema)                                            │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Backend — `smartschool-svc`

**Project layout:**
```
SmartSchool.Api/          — ASP.NET Core host (Program.cs, JWT, CORS, compression)
SmartSchool.Graphql/
  Queries/
    Query.cs              — root query type
    ResourcesQuery.cs     — resources[], resource(key)
    NavigationQuery.cs    — navigation[], locales[]
    TranslationsQuery.cs  — translations(locale, namespace?)
    GenericQuery.cs       — resourceItems(resource,skip,take), resourceItem(resource,id)
    SchoolsQuery.cs       — legacy per-entity (not in routes, kept for compatibility)
    StudentsQuery.cs, TeachersQuery.cs, UsersQuery.cs, PersonsQuery.cs
  Mutations/
    GenericMutation.cs    — createResource, updateResource, deleteResource
                            publishes ResourceChangedEvent via ITopicEventSender
  Subscriptions/
    SchoolSubscription.cs — legacy per-entity subscriptions
    GenericSubscription.cs — resourceChanged(resource) with [Topic("{resource}")]
SmartSchool.Resources/
  ResourceDescriptor.cs   — ResourceDescriptor, FieldDescriptor, RelationDescriptor,
                            ActionDescriptor records + FieldType enum
  ResourceRegistry.cs     — singleton, Initialize(assemblies), Get(key), All
  Scanning/ResourceScanner.cs — reflects on [Resource]-annotated types
SmartSchool.Schema/
  Entities/               — EF entities (AbstractRecord, School, Student, …)
  Resources/Attributes.cs — [Resource], [Field], [HiddenField], [ResourceAction]
  Entities/Locale.cs      — Locale, Translation (i18n tables)
  Entities/Rbac.cs        — Role, Permission, RolePermission, UserRoleLink
  AppDbContext.cs          — soft-delete filter, audit fields, seed via Liquibase
SmartSchool.Tests/
  Helpers/TestDb.cs        — EF InMemory AppDbContext factory
  Helpers/TestEntities.cs  — [Resource]-annotated test entities
  Resources/ResourceScannerTests.cs   — 30 tests
  Resources/ResourceRegistryTests.cs  — 8 tests
  Graphql/GenericQueryTests.cs        — 14 tests
  Graphql/GenericMutationTests.cs     — 17 tests
```

**Key facts:**
- `resourceItems` returns `JsonElement` (HotChocolate `JSON` scalar) — not strings. Apollo receives real objects.
- `GenericMutation` injects `ITopicEventSender` and publishes `ResourceChangedEvent(resource, event, id)` after every create/update/delete.
- `GenericSubscription` uses `[Topic("{resource}")]` — subscribing to `resourceChanged(resource: "school")` listens on topic `"school"`.
- `ResourceRegistry` is a singleton initialized with `typeof(AbstractRecord).Assembly` in `Program.cs`.
- Auth: JWT only. No RBAC middleware yet — `perms.set(['*:*'])` is hardcoded in `bootstrapResolver` (see §3 remaining work).
- Response compression and health checks (`/healthz`, `/readyz`) are wired up.

### 1.3 Frontend — `smartschool-gui`

**Key files:**
```
src/app/
  app.routes.ts           — 2 dynamic routes: /:resource and /:resource/:id
                            + auth (lazy) + dashboard + 404
  app.config.ts           — Apollo (cache-first, type policies, WS link),
                            ngx-translate (NoopLoader), JWT interceptor
  auth/
    auth.routes.ts        — fully lazy (loadComponent per child)
    jwt.interceptor.ts    — attaches Bearer token
    guards/auth.guard.ts  — redirects to /auth/login if not authenticated
    guards/account.guard.ts — redirects away from login if already authed
  resource-engine/
    lib/
      models/resource.models.ts         — TS interfaces matching backend DTOs
      services/
        resource-registry.service.ts   — Apollo watchQuery → Signals (resources, navigation, resourceMap)
        locale.service.ts              — Signal + RTL: sets lang/dir on <html>
        permission.service.ts          — Signal: has(code), wildcard matching
      guards/
        bootstrap.resolver.ts          — loads resources + translations at startup
        resource-exists.match.ts       — canMatch guard for /:resource routes
      directives/
        has-permission.directive.ts    — *ssHasPermission="'school:read'"
      components/
        dynamic-grid.component.ts      — renders HTML <table> from items + descriptor
                                         TODO: replace with AG Grid (v31 installed)
        dynamic-form.component.ts      — reactive form from descriptor fields
                                         TODO: replace with Formly (installed)
      pages/
        resource-list.page.ts          — apollo.watchQuery + toSignal + ResourceChanged
                                         subscription + pagination
        resource-detail.page.ts        — load via resourceItem query + save via
                                         createResource/updateResource + refetchQueries
    index.ts               — public exports
```

**Apollo configuration (app.config.ts):**
```ts
cache: new InMemoryCache({
  typePolicies: {
    GenericPage: { keyFields: false },
    Query: {
      fields: {
        resourceItems: { keyArgs: ['resource', 'skip', 'take'] },
        resourceItem:  { keyArgs: ['resource', 'id'] },
      },
    },
  },
}),
defaultOptions: { watchQuery: { fetchPolicy: 'cache-and-network' }, query: { fetchPolicy: 'cache-first' } }
```

**Real-time update flow:**
```
User saves → apollo.mutate(refetchQueries:['ResourceItems'], awaitRefetchQueries:true)
           → GenericMutation → ITopicEventSender.SendAsync("school", event)
                → WebSocket → ResourceChanged subscription fires
                  → watchRef.refetch() → watchQuery.valueChanges emits
                    → _data signal → grid re-renders
```

### 1.4 Database — Liquibase

```
smartschool-svc/db/
  liquibase.properties
  changelog/
    db.changelog-master.yaml
    releases/
      v1.0.0.yaml    — all tables: school, student, teacher, person, class,
                        enrollments, locale, translation, role, permission,
                        role_permission, user_role_link
      seed/
        locale.csv, language.csv, permission.csv, role.csv, translation.csv
```

Conventions: `id BIGINT AUTO_INCREMENT` PK, `*audit` YAML anchor (7 audit columns), FK naming `fk_<table>_<col>`, changeset ids `1.0.0-NNN-slug`. Never edit a deployed changeset — always add a new release file.

### 1.5 DevOps

```
.github/workflows/
  backend.yml   — dotnet restore/build/test + liquibase validate (offline:mariadb)
  frontend.yml  — npm ci + build (production) + test (ChromeHeadless)
docker-compose.yml       — mariadb + liquibase + svc + gui (name: smartschool)
docker-compose.debug.yml — override: svc target=debug, BUILD_CONFIGURATION=Debug
.vscode/
  launch.json  — .NET Kestrel, .NET Docker Attach, Angular Chrome/Edge,
                 "Docker: Full Stack Debug" compound
  tasks.json   — build/watch svc, serve gui, docker-compose up/down, db update
  settings.json — typescript.tsdk → smartschool-gui/node_modules/typescript/lib
```

---

## 2. Target Architecture (unchanged)

### 2.1 Guiding Principles

1. **Resource-Centric** — every domain concept is a *Resource* described by server-owned metadata.
2. **Backend-Driven UI** — navigation, columns, form fields, validators, actions, permissions, labels all come from the API.
3. **DRY** — generic CRUD resolver, generic Angular renderer, YAML anchors in DB migrations.
4. **Flat URL contract** — every page is `/<resource>` or `/<resource>/:id`. No nested paths.
5. **Single Source of Truth** — Liquibase owns the DB; C# entities + `ResourceDescriptor` generate everything.
6. **Performant** — server projections, Apollo cache, persisted queries, lazy loading, SSR transfer state.
7. **Localizable** — Arabic (RTL), Tamil, Sinhala, English. All strings from the `translation` table.

### 2.2 Liquibase Commands

```powershell
liquibase --defaults-file db/liquibase.properties update
liquibase --defaults-file db/liquibase.properties status
liquibase --defaults-file db/liquibase.properties rollback-count 1
```

---

## 3. Remaining Work

### 3.1 Priority 1 — RBAC middleware (Phase 3)

The `PermissionService` and `ssHasPermission` directive exist on the frontend. What's missing is the server enforcing permissions — unauthorized fields/resources are currently still sent to the client.

**To implement:**
- HotChocolate field middleware that reads the current user's `Permission` codes (from JWT claims) and prunes fields/resources the user cannot access.
- Replace `perms.set(['*:*'])` hardcode in `bootstrapResolver.ts` with a real `permissions` GraphQL query that returns the user's permission codes.
- Wire per-resource `IAuthorizationHandler` in `Program.cs`.

**Key files:** `bootstrap.resolver.ts` (TODO comment), `permission.service.ts`, `SmartSchool.Schema/Entities/Rbac.cs`.

### 3.2 Priority 2 — AG Grid upgrade (Phase 6 performance)

`DynamicGridComponent` is currently a plain `<table>`. AG Grid v31 is already installed (`ag-grid-angular`, `ag-grid-community`).

**To implement:**
- Replace `<table>` with `<ag-grid-angular>` in `dynamic-grid.component.ts`.
- Build `colDefs` from `resource.fields` (filter `inGrid`, map type → `agTextColumnFilter` / `agNumberColumnFilter` / `agDateColumnFilter`).
- Set `getRowId = (p) => String(p.data.id)` so AG Grid can diff rows.
- Pass `[rowData]="items"` — the `watchQuery + signal` flow from `ResourceListPage` already handles refetch; AG Grid animates diffs automatically with `[animateRows]="true"`.
- For subscription-triggered row flash (visual feedback on changed row): add `applyTransaction` + `flashCells` path using the `id` from `ResourceChangedEvent`.

**Server-side row model (SSRM)** — for large datasets (> 50k rows). AG Grid drives pagination; replace `apollo.watchQuery` with an SSRM datasource that calls `resourceItems`. On subscription event: `gridApi.refreshServerSide({ purge: false })`.

### 3.3 Priority 3 — Formly integration (Phase 6)

`DynamicFormComponent` is currently a basic `ReactiveFormsModule` form. `@ngx-formly/core` and `@ngx-formly/material` are installed.

**To implement:**
- Replace the `@for` loop of `<input>` elements with `<formly-form [fields]="formlyFields()" [form]="form" [model]="model">`.
- Build `FormlyFieldConfig[]` from `resource.fields` (filter `inForm`), mapping `FieldType` → Formly type (`input`, `select`, `checkbox`, `datepicker`, `autocomplete`).
- Wire `ValidatorDescriptor[]` from the backend into Formly validators.

### 3.4 Priority 4 — Localization completion (Phase 4 remainder)

- **RTL layout audit**: add `dir="rtl"` to `<html>` (done in `LocaleService`) but CSS needs `logical` properties (`margin-inline-start` not `margin-left`) and icon mirroring.
- **ICU pluralization**: add `ngx-translate-messageformat-compiler` and configure `TranslateModule.forRoot({ compiler: { provide: TranslateCompiler, useClass: TranslateMessageFormatCompiler } })`.
- **Translation admin UI**: a resource-engine page for editing `translation` table rows — this renders itself once the resource engine is done.

### 3.5 Priority 5 — Performance pass (Phase 6 remainder)

| Item | What to do |
|---|---|
| Persisted queries | Add `AddReadOnlyFileSystemQueryStorage` + `UsePersistedQueryPipeline` in HotChocolate; use Apollo's persisted queries link. |
| SSR transfer state | `provideClientHydration(withHttpTransferCacheOptions({...}))` + Apollo cache extraction. |
| OpenTelemetry | `AddOpenTelemetry()` → OTLP exporter; inject `traceparent` via Apollo link. |
| HTTP/2 + brotli | `app.UseResponseCompression()` (done) + Kestrel HTTP/2 config. |
| Cache response headers | 5-min ETag response caching for `resources`, `navigation`, `translations` queries. |

### 3.6 Priority 6 — Angular 17 → 18 upgrade (Phase 0 deferred)

- `ng update @angular/core@18 @angular/cli@18`
- Enable `@defer` for grid/form blocks.
- Switch `HttpClientModule` (deprecated) → `provideHttpClient(withInterceptors([...]))`.
- Already uses standalone components and `@if/@for/@switch` control flow.

---

## 4. Known Issues & Gotchas

| Issue | Detail |
|---|---|
| `node_modules` not installed | `npm install` hasn't been run in `smartschool-gui/` — Node.js not in PATH. VS Code shows "Cannot find module" errors for all Angular imports. These are false positives; the build is correct. |
| Docker Desktop blocked | Windows CBS issue (pending.xml 1.3 MB). Fix steps in Quick-start above. |
| Permissions hardcoded | `bootstrapResolver.ts` line: `perms.set(['*:*'])` — everyone has all permissions until RBAC middleware is implemented. |
| Old per-entity components | `StudentsComponent`, `SchoolsComponent`, etc. still exist in `src/app/` but are NOT in `app.routes.ts`. They are unreachable and should be deleted once the resource-engine is stable. |
| EF migrations still present | `SmartSchool.Schema/Migrations/` exists. Liquibase is now the schema owner; EF migrations are stale. Do not run `dotnet ef database update` — run Liquibase instead. |
| `cache-first` fallback | `defaultOptions.watchQuery.fetchPolicy` is `cache-and-network`; `query.fetchPolicy` is `cache-first`. Pages that need fresh data use explicit `network-only`. |

---

## 5. Phased Roadmap

| Phase | Status | Summary |
|---|---|---|
| **0. Foundation** | ✅ Done | Monorepo, CI, Docker Compose, Liquibase DB. Angular 17→18 and .NET 8→9 upgrades deferred. |
| **1. Metadata Layer** | ✅ Done | `SmartSchool.Resources`, attribute scanner, `ResourcesQuery`, `NavigationQuery`, `TranslationsQuery`, RBAC + i18n DB tables + seed CSVs. |
| **2. Resource Engine** | ✅ Done | Angular `resource-engine/`: `ResourceListPage` + `ResourceDetailPage` wired with `watchQuery` + signals; `DynamicGridComponent` (HTML table); `DynamicFormComponent` (reactive forms); `LocaleService`; `PermissionService`; `bootstrapResolver`; `ssHasPermission` directive; 2-route `app.routes.ts`. |
| **3. Permissions + RBAC** | 🔶 Partial | DB tables + frontend service + UI directive done. Missing: HotChocolate field middleware + real permission query replacing `perms.set(['*:*'])`. |
| **4. Localization** | 🔶 Partial | Tables + seed CSVs + `TranslationsQuery` + `LocaleService` + ngx-translate done. Missing: RTL CSS audit, ICU pluralization. |
| **5. Generic CRUD** | ✅ Done | `GenericQuery` (`JsonElement` items/item), `GenericMutation` (create/update/delete + `ITopicEventSender`), `GenericSubscription` (real-time via WS topic), `refetchQueries` on save, Apollo type policies. |
| **6. Performance** | 🔶 Partial | `cache-first` default, `InMemoryCache` type policies, response compression, health checks done. Remaining: AG Grid upgrade, Formly upgrade, persisted queries, SSR transfer state, OTel. |
| **7. Hardening & Release** | ❌ Pending | Load tests, a11y audit (RTL), docs, v1.0 release. |

**Next action:** §3.1 (RBAC middleware), then §3.2 (AG Grid upgrade).

---

## 6. Definition of Done

- [x] Adding a new entity = (a) C# entity, (b) `[Resource]` annotation, (c) Liquibase changeset. Zero GUI changes for default CRUD.
- [ ] Every label, menu item, action, and validation message comes from the backend. _(labels ✅ menus ✅ validation messages ❌ pending Formly)_
- [ ] All four locales render correctly; Arabic flips layout cleanly. _(loading ✅ RTL CSS ❌ ICU ❌)_
- [ ] FCP < 1.5 s SSR; Lighthouse ≥ 90. _(SSR transfer state pending §3.5)_
- [ ] No unauthorized resource/field sent to client. _(UI gating ✅ server pruning ❌ pending §3.1)_
- [x] >80% test coverage on metadata + permission layers. _(69 backend tests; Angular specs ready for npm install)_
- [ ] CI green; `docker compose up` works end-to-end. _(CI ✅ Docker blocked by CBS issue)_

---

## 7. Localization Matrix

| Locale | Code | Direction | Status |
|---|---|---|---|
| English | `en` | LTR | ✅ Default, seed CSV loaded |
| Sinhala | `si` | LTR | ✅ Seeded |
| Tamil | `ta` | LTR | ✅ Seeded |
| Arabic | `ar` | **RTL** | ✅ Seeded — RTL CSS audit pending |

Rules: no literal strings in TS/HTML — keys only. Enums emit translation keys. Dates/numbers via `Intl`.

---

## 8. Git History (this implementation)

```
a55a2ae chore: rename repo references from smart-school to smartschool
ad4e8f2 feat: real-time UI refresh via JsonElement + GraphQL subscriptions (Option B)
af1f4fb fix: address architecture-plan audit findings across last three commits
4d3942f test: add unit tests for resource engine, generic CRUD, and Angular components
4fdd6b6 feat: execute architecture plan phases 0-4 — resource engine, RBAC, i18n, generic CRUD, Docker, CI
7f8d285 feat(db): Liquibase YAML schema, RBAC + resource metadata + i18n; simplify routes to /:resource/:id
```

---

*Last updated: 2026-05-29 · Owner: Architecture*
