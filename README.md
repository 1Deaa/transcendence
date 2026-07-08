# HrmSystem

Multi-tenant HRM SaaS platform — .NET 10, Clean Architecture, DDD, CQRS.

| Layer | Project |
|---|---|
| Domain | `src/Core/HrmSystem.Domain` |
| Application | `src/Core/HrmSystem.Application` |
| Infrastructure | `src/Infrastructure/HrmSystem.Infrastructure` |
| Web (REST API) | `src/Web/HrmSystem.Web` |
| Client (Blazor WASM) | `src/Client/HrmSystem.Client` |

## Phase 1 modules

- **Health check & status page system** — public status page, authenticated health dashboard, automated SQL Server backups, disaster recovery runbook (`docs/disaster-recovery-runbook.md`).
- **Advanced analytics dashboard** — interactive charts (ApexCharts), SignalR real-time updates, PDF/CSV export, date-range filters.
- **Data export/import** — JSON/CSV/XML export, validated CSV import with background processing, bulk operations.

## Getting started

```powershell
docker compose up -d --build
# API:    http://localhost:5000  (Scalar docs at /scalar)
# Client: http://localhost:5100
```

See `docs/architecture.md` for architectural decisions.
# HRM-System.42-Project
