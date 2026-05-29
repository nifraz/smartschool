# SmartSchool Database (Liquibase)

```
db/
├─ liquibase.properties           # local connection config (override via env vars)
└─ changelog/
   ├─ db.changelog-master.yaml    # entry point; includes every release in order
   └─ releases/
      ├─ v1.0.0.yaml              # baseline schema (DRY via YAML anchors)
      └─ seed/                    # CSV seed files (idempotent via loadUpdateData)
         ├─ locale.csv
         ├─ language.csv
         ├─ permission.csv
         ├─ role.csv
         └─ translation.csv
```

## Conventions

| Concern | Rule |
|---|---|
| PK | `id BIGINT AUTO_INCREMENT` (lookup tables: `code VARCHAR(8|32)`) |
| Audit | every business table includes 7 audit columns via `*audit` anchor |
| FK column | `<entity>_id` |
| FK constraint | `fk_<table>_<column>` |
| Index | `ix_<table>_<col>` · unique: `ux_<table>_<col>` |
| ChangeSet id | `<version>-<NNN>-<slug>` (e.g. `1.0.0-040-school`) |
| Author | `smartschool` (or a contributor handle) |
| Immutability | NEVER edit a deployed changeset — add a new release file instead |
| Seed | CSV + `loadUpdateData` + `runOnChange: true` (idempotent) |

## Adding a new release

1. Create `db/changelog/releases/v<X.Y.Z>.yaml`.
2. Add the include at the bottom of `db.changelog-master.yaml` (or rely on `includeAll` once enabled).
3. Use the `*audit` and `*pk` anchors at the top of the file for consistency.
4. Group FKs into a single `*-080-foreign-keys` changeset at the end.

## Running

```powershell
# from the smartschool-svc folder
liquibase --defaults-file db/liquibase.properties update
liquibase --defaults-file db/liquibase.properties status
liquibase --defaults-file db/liquibase.properties rollback-count 1
liquibase --defaults-file db/liquibase.properties validate
```

## Why Liquibase (vs EF Migrations)

- DB is the single source of truth — multiple services / tools can share it.
- YAML is reviewable and tool-agnostic.
- Built-in `contexts` (`local`, `staging`, `prod`) and `labels` for selective deploys.
- Out-of-the-box rollback, status, diff, snapshot, drop-all.
- Works with the planned RESOURCE-METADATA-driven backend without coupling to EF.
