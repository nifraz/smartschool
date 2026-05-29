# SmartSchool

Monorepo containing the SmartSchool platform.

```
smart-school/
├── ARCHITECTURE_PLAN.md          # the full plan
├── docker-compose.yml            # mariadb + liquibase + svc + gui
├── smartschool-svc/              # .NET 8 + HotChocolate + EF Core (MariaDB)
│   ├── SmartSchool.Api/          # ASP.NET Core host
│   ├── SmartSchool.Graphql/      # HotChocolate types, generic CRUD, metadata queries
│   ├── SmartSchool.Resources/    # ResourceRegistry + scanner
│   ├── SmartSchool.Schema/       # EF entities, AppDbContext, [Resource]/[Field] attributes
│   ├── SmartSchool.Service/      # domain services
│   ├── SmartSchool.Utility/      # helpers
│   └── db/                       # Liquibase YAML migrations + seeds
└── smartschool-gui/              # Angular 18 + Apollo + Material + Formly
    └── src/app/
        ├── app.routes.ts         # 2 dynamic routes cover every resource
        ├── resource-engine/      # generic registry, dynamic grid/form, dynamic pages
        ├── auth/                 # login/register/verify
        ├── dashboard/            # static landing page
        └── shared/               # legacy widgets (being phased out)
```

## Quick start

```powershell
docker compose up --build
# GUI    → http://localhost:4200
# API    → http://localhost:5000/graphql
# DB     → mariadb on :3306 (root/root)
```

The `liquibase` service applies the baseline schema and seed data before `svc` starts.

## Adding a new resource (the whole flow)

1. **Add an EF entity** in `SmartSchool.Schema/Entities/Foo.cs` deriving from `AbstractRecord`.
2. **Annotate** it: `[Resource("foo", Plural = "foos", Module = "academic")]` and `[Field]` on properties you want to expose.
3. **Add a Liquibase changeset** for the new table in a new release file under `smartschool-svc/db/changelog/releases/v<X.Y.Z>.yaml`.
4. **Add `DbSet<Foo>`** to `AppDbContext`.
5. **Restart svc + run liquibase**. The GUI now has `/foos` and `/foos/:id` automatically — no Angular changes needed.

## See also

- [`ARCHITECTURE_PLAN.md`](./ARCHITECTURE_PLAN.md) — the full plan
- [`smartschool-svc/db/README.md`](./smartschool-svc/db/README.md) — Liquibase conventions
