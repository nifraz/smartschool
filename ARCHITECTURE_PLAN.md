# SmartSchool — Backend-Driven, Resource-Centric Architecture Plan

> Target outcome: a single, modular, modern monorepo (`smart-school`) in which the **backend is the single source of truth** for data, behavior, navigation, forms, grids, validation, permissions, and localization. The Angular client becomes a **thin, generic renderer** of resources delivered over GraphQL.

---

## 1. Current State (Audit Summary)

### 1.1 Frontend — `smartschool-gui` (Angular 17)
- Apollo + Angular Material + Formly + AG Grid + ngx-toastr + ngx-bootstrap.
- **Hard-coded routes** in `app.routes.ts` (~30 entries) — one component per entity (`StudentsComponent`, `SchoolsComponent`, `ClassDetailsComponent`, ...).
- **Hard-coded GraphQL** in `shared/queries.ts`, `mutations.ts`, `subscriptions.ts` (~490+ LoC each).
- Generic primitives already exist but are under-used:
  - `shared/components/grid`, `record`, `record-form`, `graphql-data-grid`, `graphql-record-form`, `report`, `autocomplete-type`.
- No i18n; English-only strings inline in HTML/TS.
- `fetchPolicy: 'no-cache'` everywhere → cache layer wasted.
- SSR hydration is on but route-level data resolvers are missing.
- No lazy loading (every feature eagerly imported into root routes).

### 1.2 Backend — `smartschool-svc` (.NET 8 + HotChocolate + EF Core + MariaDB)
- Clean layering: `Schema` (EF entities) / `Service` / `Graphql` / `Api` / `Utility`.
- Entities inherit `AbstractRecord` (Id, audit fields, soft-delete). Good base.
- GraphQL: `Query`/`Mutation` split by entity (`SchoolsQuery`, `StudentMutation`, ...). Projections, filtering, sorting, subscriptions enabled.
- Auth: JWT (`AuthService`), but no policy/permission model — only role enum (`UserRole`).
- No localization tables. No metadata/resource registry. No tenancy.
- Mappers + DataLoaders present (good base for batching).

### 1.3 Pain Points
| Area | Problem |
|---|---|
| Adding a new entity | Requires changes in ~10 files across both repos. |
| Forms/Grids | Schema duplicated in C# entity + GraphQL + TS query + Formly config + grid columns. |
| Permissions | Implicit; mixed in controllers/queries; no UI hiding driven by server. |
| Localization | None. |
| Routing | Static; cannot add a module without redeploying GUI. |
| Caching | Disabled. |

---

## 2. Target Architecture

### 2.1 Guiding Principles
1. **Resource-Centric** — every domain concept (School, Student, Class, …) is a *Resource* described by **server-owned metadata**.
2. **Backend-Driven UI** — navigation, grid columns, form fields, validators, actions, permissions, and labels are emitted by the API; the GUI renders them.
3. **One way to do everything (DRY)** — generic CRUD resolver, generic Angular renderer, YAML anchors in DB migrations. Repetition is a bug.
4. **Flat URL contract** — every page is `/<resource>` or `/<resource>/:id`. No nested paths, ever.
5. **Single Source of Truth** — Liquibase YAML owns the DB; C# entities + a `ResourceDescriptor` generate GraphQL schema, REST endpoints, OpenAPI, TS types, and UI metadata.
6. **Performant** — server projections, DataLoader batching, Apollo normalized cache, persisted queries, route-level lazy loading, SSR with transfer state.
7. **Modular** — feature modules on both sides; pluggable resource modules on the backend; lazy Angular standalone routes resolved at runtime.
8. **Modern** — Angular 18 LTS, Signals, standalone components, Control Flow, `@defer`, HotChocolate 14, .NET 8/9, EF Core 8, Liquibase 4.
9. **Localizable** — Arabic (RTL), Tamil, Sinhala, English from day one. All user-facing strings come from the backend `translation` table.

### 2.2 High-Level Diagram
```
┌────────────────────────────── smart-school (monorepo) ──────────────────────────────┐
│                                                                                     │
│  smartschool-svc (.NET 8)                       smartschool-gui (Angular 18)        │
│  ┌──────────────────────────────┐               ┌──────────────────────────────┐    │
│  │ Schema (EF entities + attrs) │──┐            │ core/         (bootstrap)    │    │
│  │ Service (domain logic)       │  │            │ resource-engine/             │    │
│  │ Graphql (HotChocolate)       │  │  GraphQL   │   - router-builder           │    │
│  │ Resources (metadata layer) ──┼──┼──────────▶ │   - dynamic-grid             │    │
│  │ Localization (translations)  │  │  WS/Subs   │   - dynamic-form (Formly)    │    │
│  │ Auth (JWT + permissions)     │  │            │   - permission-directive     │    │
│  │ Api (host + REST shim)       │──┘            │   - i18n-loader              │    │
│  └──────────────────────────────┘               │ resources/ (per-resource UI  │    │
│           │                                     │   overrides — optional)       │    │
│           ▼                                     └──────────────────────────────┘    │
│       MariaDB                                                                       │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 3. Backend Plan (`smartschool-svc`)

### 3.1 New Project: `SmartSchool.Resources`
Owns all metadata that drives the UI.

```csharp
public sealed class ResourceDescriptor {
    public string Key { get; init; }                 // "school", "student"
    public string PluralKey { get; init; }           // "schools"
    public string IconKey { get; init; }             // "mat:school"
    public string LabelKey { get; init; }            // i18n key
    public string Module { get; init; }              // "academic", "people"
    public PermissionSet Permissions { get; init; }  // read/create/update/delete/custom
    public IReadOnlyList<FieldDescriptor> Fields { get; init; }
    public IReadOnlyList<ActionDescriptor> Actions { get; init; }
    public IReadOnlyList<ViewDescriptor> Views { get; init; }     // list / detail / form / report
    public RouteDescriptor Routes { get; init; }
    public RelationDescriptor[] Relations { get; init; }
}

public sealed class FieldDescriptor {
    public string Key { get; init; }
    public string LabelKey { get; init; }
    public FieldType Type { get; init; }    // String, Int, Date, Enum, Ref, Money, Phone, ...
    public bool Required { get; init; }
    public bool Unique { get; init; }
    public string? RefResource { get; init; }
    public ValidatorDescriptor[] Validators { get; init; }
    public DisplayHints Display { get; init; } // grid? form? width? mask? RTL-aware?
    public PermissionExpression? VisibleWhen { get; init; }
}
```

Two ways to populate descriptors:
1. **Convention + attributes** on EF entities (`[Resource("school")]`, `[Field(Label="School Name", InGrid=true)]`).
2. **Override** in `*ResourceProfile.cs` for advanced cases (custom views, computed fields).

### 3.2 GraphQL Schema Additions
```graphql
type Query {
  resources: [Resource!]!                 # all resources current user can see
  resource(key: String!): Resource
  navigation(scope: String): [NavNode!]!  # dynamic, permission-filtered tree
  translations(locale: String!, ns: [String!]): TranslationBundle!
  permissions: PermissionBundle!
}
```
`Resource` exposes fields, views, actions, routes — exactly what the GUI needs to render any page.

### 3.3 Generic CRUD Layer
Replace per-entity `*Query.cs` / `*Mutation.cs` with a **convention-driven generic resolver**:
```csharp
[ExtendObjectType<Query>]
public class GenericQuery {
    public IQueryable<T> Items<T>(...) where T : AbstractRecord => ...;
    public Task<T?> Item<T>(long id, ...) where T : AbstractRecord => ...;
}
[ExtendObjectType<Mutation>]
public class GenericMutation {
    public Task<T> Create<T>(JsonElement input, ...);
    public Task<T> Update<T>(long id, JsonElement input, ...);
    public Task<bool> Delete<T>(long id, ...);
    public Task<TOut> Invoke<TOut>(string resource, string action, JsonElement input, ...);
}
```
Per-entity files are deleted; specialized logic moves into **`I*Policy`** + **`I*Service`** with attribute-based registration.

### 3.4 Permissions & Multi-Tenancy
- Introduce `Permission`, `Role`, `RolePermission`, `UserRole` tables (replace flat enum).
- Permission codes are `resource:action` (e.g. `school:update`, `class.teacher:assign`).
- All queries pass through a `PermissionMiddleware` (HotChocolate field middleware) that prunes unauthorized fields/actions.
- Tenant id (`SchoolId` / `Division`) becomes an ambient filter (`Microsoft.EntityFrameworkCore.Query.IQueryFilter`).

### 3.5 Localization Module
- Tables: `Locale (code, name, isRtl)`, `Translation (locale, namespace, key, value)`.
- Seeded with **en, si, ta, ar** (Arabic = `isRtl=true`).
- Admin UI (itself rendered by the resource engine!) to edit translations.
- `GET /i18n/{locale}.{ns}.json` and GraphQL `translations` query both available; client caches with ETag.
- All `LabelKey` fields in descriptors are resolved via this table.

### 3.6 Performance Backend
- Keep `AddPooledDbContextFactory` + `RegisterDbContext(Pooled)`.
- Add **persisted queries** (`AddReadOnlyFileSystemQueryStorage`) + **automatic query persistence**.
- Add **response caching** for `resources`, `navigation`, `translations` (5 min + tag-based eviction on metadata change).
- Output cache for static descriptors; in-memory `IMemoryCache` for permission lookups per user.
- Move `Newtonsoft.Json` → `System.Text.Json` (HotChocolate native).
- Enable **HTTP/2 + gzip/brotli**; `app.UseResponseCompression()`.
- Health checks (`/healthz`, `/readyz`) + OpenTelemetry traces (OTLP exporter).

### 3.7 Auth Hardening
- Refresh tokens (rotating) stored hashed.
- Argon2id for password hashing.
- 2FA TOTP optional.
- Per-resource policies via `IAuthorizationHandler` driven by `Permission` codes.

### 3.8 Migration Steps (Backend)
1. **Adopt Liquibase** as the schema owner (see §3.9). Generate the v1.0.0 baseline from the current EF model; switch EF to **database-first / no-migrations** mode.
2. Add `SmartSchool.Resources` + attribute scanner; expose `resources`/`navigation` queries (no behavior change yet).
3. Locale/Translation tables are already in the baseline; expose `translations` query.
4. Replace `UserRole` enum with the new RBAC tables (already created); backfill data in a Liquibase changeset.
5. Implement generic CRUD; route old `*Query`/`*Mutation` calls through it; deprecate originals.
6. Remove deprecated files; tighten schema.
7. Add caching, persisted queries, OTel, compression.

### 3.9 Database Migrations — Liquibase (YAML)

Schema is owned by **Liquibase**, not EF migrations. EF Core is configured for **runtime queries only**; the database is the single source of truth and can be evolved independently of the .NET build.

```
smartschool-svc/db/
├─ liquibase.properties              # local connection (env vars override)
└─ changelog/
   ├─ db.changelog-master.yaml       # entry point; includes every release
   └─ releases/
      ├─ v1.0.0.yaml                 # baseline (DRY via YAML anchors)
      └─ seed/                       # CSV seeds via loadUpdateData
         ├─ locale.csv  language.csv  permission.csv  role.csv  translation.csv
```

**DRY mechanism** — YAML anchors at the top of each release file:
```yaml
x-audit: &audit
  - column: { name: notes,                 type: VARCHAR(512) }
  - column: { name: created_time,          type: DATETIME(6) }
  - column: { name: last_modified_time,    type: DATETIME(6) }
  - column: { name: deleted_time,          type: DATETIME(6) }
  - column: { name: created_user_id,       type: BIGINT }
  - column: { name: last_modified_user_id, type: BIGINT }
  - column: { name: deleted_user_id,       type: BIGINT }

x-pk: &pk
  column: { name: id, type: BIGINT, autoIncrement: true, constraints: { primaryKey: true, nullable: false } }
```
Every table reuses them:
```yaml
- createTable:
    tableName: school
    columns:
      - { <<: *pk }
      - column: { name: name, type: VARCHAR(256), constraints: { nullable: false } }
      # ...
      - *audit
```

**Conventions**

| Concern | Rule |
|---|---|
| PK | `id BIGINT AUTO_INCREMENT` (lookups: `code VARCHAR(8|32)`) |
| Audit | 7 columns appended via `*audit` anchor |
| FK column / constraint | `<entity>_id` / `fk_<table>_<col>` |
| Index | `ix_<table>_<col>` · unique `ux_<table>_<col>` |
| ChangeSet id | `<version>-<NNN>-<slug>` (e.g. `1.0.0-040-school`) |
| Foreign keys | grouped into a single `*-080-foreign-keys` changeset per release |
| Seed | CSV + `loadUpdateData` + `runOnChange: true` (idempotent) |
| Immutability | NEVER edit a deployed changeset — add a new release file |

**Commands**
```powershell
liquibase --defaults-file db/liquibase.properties update
liquibase --defaults-file db/liquibase.properties status
liquibase --defaults-file db/liquibase.properties rollback-count 1
```

---

## 4. Frontend Plan (`smartschool-gui`)

### 4.1 Upgrade Baseline
- Angular **17 → 18 LTS**; enable Signals, new control flow (`@if/@for/@switch`), `@defer`.
- Replace eager `HttpClientModule` import with `provideHttpClient(withInterceptors([...]))`.
- Switch Apollo `fetchPolicy` default to **`cache-first`** + explicit `network-only` per case.
- Enable **persisted queries** link + **SSR transfer state** link.
- Replace `ngx-bootstrap` with Material-only to reduce bundle (or vice-versa — pick one).
- Add **Nx workspace** (or Angular library projects) for modularization.

### 4.2 New Library: `@smartschool/resource-engine`
| Piece | Purpose |
|---|---|
| `ResourceRegistryService` | Loads `resources` + `navigation` once, caches with TTL, exposes Signals. |
| `DynamicRoutesBuilder` | Builds `Route[]` at app bootstrap from `Resource.routes`. Standalone, lazy. |
| `<ss-dynamic-grid>` | Renders AG Grid from `Resource.views.list` (columns, filters, server pagination). |
| `<ss-dynamic-form>` | Renders Formly fields from `Resource.views.form` + descriptor validators. |
| `<ss-dynamic-detail>` | Tabs/sections + related resources, all from metadata. |
| `<ss-action-button>` | Renders `ActionDescriptor`, checks permission, calls `invoke` mutation. |
| `ssHasPermission` directive | `*ssHasPermission="'school:update'"` → server-evaluated. |
| `PermissionService` | Reactive permission set; updates on login/role change. |

### 4.3 Routing — Pure `:resource/:id` Convention
**Hard rule:** every URL is `/<resource>` or `/<resource>/:id`. Nothing else. Relations are query/state, not route segments. This collapses ~30 hand-written routes into **2 generic ones**.

```ts
// app.routes.ts (final, complete file)
export const routes: Routes = [
  { path: 'auth', loadChildren: () => import('./auth/auth.routes').then(m => m.routes) },
  {
    path: '',
    canActivate: [authGuard],
    resolve: { _bootstrap: bootstrapResolver },     // loads resources + permissions + i18n
    children: [
      { path: '',          pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./pages/dashboard/dashboard.component') },
      // ── The entire app, dynamically ──
      { path: ':resource',     loadComponent: () => import('@smartschool/resource-engine').then(m => m.ResourceListPage),   canMatch: [resourceExistsMatch] },
      { path: ':resource/:id', loadComponent: () => import('@smartschool/resource-engine').then(m => m.ResourceDetailPage), canMatch: [resourceExistsMatch] },
      { path: '**', loadComponent: () => import('./pages/not-found/not-found.component') },
    ],
  },
];
```

- `resourceExistsMatch` looks up the `:resource` segment in the `ResourceRegistryService` (loaded once at bootstrap). Unknown resources fall through to 404.
- `ResourceDetailPage` reads `Resource.views.detail.tabs[]` — *related lists, sub-forms, history* — all rendered inside the detail page. **Drill-downs into related items just navigate to `/<relatedResource>/<id>`**, not nested URLs.
- Result: adding a new entity = inserting one row in `resource` (+ rows in `resource_field`, `resource_view`). Zero route edits.

### 4.4 Localization
- Adopt **`@angular/localize`** *or* **`@ngx-translate/core`** (recommend ngx-translate for runtime locale switch).
- Loader fetches from backend `translations(locale, ns)`; cached in `localStorage` + ETag revalidation.
- `LocaleService` exposes `currentLocale` Signal; toggles `dir="rtl"` on `<html>` for `ar`.
- All component templates use `{{ 'school.name.label' | translate }}`; descriptors carry only keys.
- Date/number formatting via Angular `DATE_PIPE_DEFAULT_OPTIONS` + Intl APIs.
- Font stack: `Noto Sans Arabic`, `Noto Sans Tamil`, `Noto Sans Sinhala`, `Inter`.

### 4.5 Performance Frontend
| Lever | Action |
|---|---|
| Bundle | Standalone + `@defer` for grids/forms; route-level lazy. |
| Cache | Apollo `InMemoryCache` with type policies keyed by `id`; default `cache-first`. |
| Network | Persisted queries link; batching link for parallel queries. |
| SSR | `provideClientHydration(withHttpTransferCacheOptions({...}))`; SSR Apollo cache extraction. |
| Images | `NgOptimizedImage` everywhere. |
| Change detection | OnPush + Signals across resource-engine. |
| Forms | `updateOn: 'blur'` for heavy forms. |
| Grids | Server-side row model in AG Grid; pull only the page. |

### 4.6 Folder Layout (Angular)
```
src/app/
  core/                 # auth, http, error handling, i18n setup
  layout/               # shell, navbar, sidebar (driven by navigation query)
  resource-engine/      # the generic renderer library
  resources/            # OPTIONAL per-resource overrides (custom widgets, dashboards)
    school/
    student/
  pages/                # truly static pages (dashboard, login, 404)
  shared/               # pure presentational components (kept)
```

### 4.7 Migration Steps (Frontend)
1. Upgrade to Angular 18; introduce Signals & new control flow incrementally.
2. Build `resource-engine` library; cover one resource end-to-end (e.g., `School`) behind a feature flag.
3. Add i18n + locale switcher (with backend stub).
4. Migrate resources one-by-one: delete per-entity components & queries; declare metadata only.
5. Remove `app.routes.ts` static entries; switch to `dynamicRoutes()`.
6. Turn on Apollo cache, persisted queries, SSR transfer state.

---

## 5. Cross-Cutting Concerns

### 5.1 Code Generation
- Backend exposes `/schema.graphql` + `/resources.json`.
- GUI `npm run generate` produces:
  - Strongly-typed TS for generic `items/item/create/update/delete/invoke` operations.
  - Resource key union type (`type ResourceKey = 'school' | 'student' | ...`).
- Optional: generate Storybook stubs per resource view.

### 5.2 Validation
- Single rule set authored on the C# entity (DataAnnotations + custom).
- Emitted as `ValidatorDescriptor[]` per field → consumed by Formly on the client.
- Server always re-validates (never trust client).

### 5.3 Observability
- Backend: OpenTelemetry (traces, metrics, logs) → OTLP → Grafana/Tempo/Loki.
- Frontend: lightweight RUM (web-vitals + custom GraphQL latency).
- Correlate via `traceparent` header (Apollo link injects).

### 5.4 Testing
- Backend: xUnit + Testcontainers (MariaDB) for integration; snapshot tests for GraphQL schema.
- Frontend: Vitest/Jest + Angular Testing Library; Playwright for E2E covering resource-engine flows once (all resources benefit).

### 5.5 CI/CD
- GitHub Actions:
  - `backend.yml`: build, test, EF migrations check, container image push.
  - `frontend.yml`: build, lint, test, Lighthouse budget, container image push.
  - `release.yml`: tags create coordinated releases for both subtrees.
- Docker Compose for dev (api + mariadb + gui + otel-collector).

---

## 6. Localization Matrix

| Locale | Code | Direction | Notes |
|---|---|---|---|
| English | `en` | LTR | Default fallback. |
| Sinhala | `si` | LTR | Noto Sans Sinhala. |
| Tamil | `ta` | LTR | Noto Sans Tamil. |
| Arabic | `ar` | **RTL** | Auto-flip layout via `dir="rtl"`; mirror icons & paddings; `logical` CSS properties (`margin-inline-start`). |

Rules:
- No literal strings in TS/HTML — only keys.
- All enums (`Sex`, `Grade`, `SchoolType`, `EnrollmentStatus`, …) emit translation keys, not raw values.
- Numbers/dates via `Intl`, calendar locale per user preference.
- Pluralization via ICU MessageFormat (`ngx-translate-messageformat-compiler`).

---

## 7. Phased Roadmap

| Phase | Duration | Deliverables |
|---|---|---|
| **0. Foundation** | 1 wk | Monorepo cleanup, CI, Docker Compose, Angular 18 upgrade, .NET 9 ready. |
| **1. Metadata Layer** | 2 wks | `SmartSchool.Resources`, `resources`/`navigation` queries, attribute scanner. |
| **2. Resource Engine** | 2 wks | Angular library: dynamic routes, dynamic grid, dynamic form, permission directive. Pilot: `School`. |
| **3. Permissions + RBAC** | 1 wk | New tables, middleware, server-driven permission bundle, GUI gating. |
| **4. Localization** | 1 wk | Translation tables, GraphQL + REST endpoints, GUI loader, 4 locales seeded, RTL audit. |
| **5. Generic CRUD** | 2 wks | Replace per-entity queries/mutations; migrate all resources to engine. |
| **6. Performance Pass** | 1 wk | Apollo cache, persisted queries, SSR transfer state, response compression, OTel, AG Grid SSRM. |
| **7. Hardening & Release** | 1 wk | Load tests, accessibility audit (RTL + a11y), docs, v1.0 release. |

**Total: ~11 weeks** to a fully dynamic, resource-centric SmartSchool.

---

## 8. Definition of Done

- [ ] Adding a new entity = (a) add C# entity, (b) annotate, (c) run migration. **Zero** GUI changes required for default CRUD.
- [ ] Every label, menu item, action, and validation message comes from the backend.
- [ ] All four locales render correctly; Arabic flips the UI cleanly.
- [ ] First contentful paint < 1.5 s on broadband SSR; Lighthouse perf ≥ 90.
- [ ] No unauthorized resource/field is ever sent to the client.
- [ ] >80% backend test coverage on the metadata + permission layers.
- [ ] CI green; container images published; `docker compose up` works end-to-end.

---

*Document owner:* Architecture · *Last updated:* 2026-05-29
