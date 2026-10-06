# ft_transcendence — module map and point calculation

Subject: *ft_transcendence* (v21.1). Requirement: **14 points minimum**
(Major = 2 pts, Minor = 1 pt). Only fully functional, demonstrated modules count.

| # | Module | Type | Pts | Evidence (where to look / demo) | Owner |
| --- | --- | --- | --- | --- | --- |
| 1 | Frontend + backend framework — Blazor WASM + ASP.NET Core | Major | 2 | Whole stack (`src/Client`, `src/Web`); framework-definition note in README | <LOGIN-?> |
| 2 | Real-time features — WebSockets (SignalR) | Major | 2 | `/hubs/*`; chat, notification bell, analytics refresh, import progress; WS handshake auth via `?access_token` | <LOGIN-?> |
| 3 | ORM for the database — EF Core 10 | Minor | 1 | All writes through repositories + UnitOfWork (`Persistence/Repositories`) | <LOGIN-?> |
| 4 | Advanced permissions system | Major | 2 | 4 roles, role→permission matrix, `[HasPermission]` per endpoint, admin panel (view/edit/role/suspend/**delete** users) | <LOGIN-?> |
| 5 | Advanced analytics dashboard with data visualization | Major | 2 | `/dashboard`; 4 chart types, date filters, SignalR refresh, CSV/PDF export (QuestPDF) | <LOGIN-?> |
| 6 | Data export and import | Minor | 1 | JSON/CSV/XML exports; background CSV import with live progress (`/imports`) | <LOGIN-?> |
| 7 | Health check + status page + automated backups + DR | Minor | 1 | `/health`, `/status`, Quartz backups, `docs/disaster-recovery-runbook.md` | <LOGIN-?> |
| 8 | Module of choice (Major) — multi-tenant SaaS isolation | Major | 2 | Four-layer isolation (filters, interceptors, tenant-owned aggregates, Dapper guard tests); justification in README | <LOGIN-?> |
| 9 | Advanced search — filters, sorting, pagination | Minor | 1 | Employees directory: search + department/status filters + sort control + `sortBy`/`sortDirection` API params + offset pagination | <LOGIN-?> |
| | **Total** | | **14** | | |

## Buffer candidates (bonus, optional — subject recommends aiming above 14)

- **Support for additional browsers** (Minor, 1 pt): full compatibility with Firefox +
  Edge, documented limitations, consistent UI/UX. Test on a fresh machine before
  claiming; only claim what you can demo.

## Notes for evaluation

- Every claim above is demo-able from a fresh `docker compose up -d --build` (seeded
  demo data included).
- Run one backup on eval day (`POST /api/backups/run` as platform admin) so the status
  page reports Healthy from the start.
- The bug-audit files in the repository root document manual evaluation passes; keep
  them current or remove them.
