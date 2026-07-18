# HrmSystem

Multi-tenant HRM SaaS platform — .NET 10 Clean Architecture backend + standalone Blazor WebAssembly frontend (Vuexy theme), built as a portfolio-grade reference implementation.

## What's inside

| Module | Highlights |
| --- | --- |
| **Core HRM slice** | Tenants, Departments, Employees, Attendance, Leave Requests — CQRS vertical slices, Result pattern, strongly-typed IDs, DDD value objects |
| **Analytics dashboard** (major) | Dapper read models, Redis-cached queries, ApexCharts UI, SignalR live refresh on domain events, CSV/PDF export (QuestPDF) |
| **Health & status page** (minor) | Liveness/readiness probes, per-component snapshots + uptime history, anonymous status page with SignalR push, automated SQL backups + DR runbook |
| **Export / import** (minor) | Content-negotiated exports (JSON/CSV/XML via `Accept` or `?format=`), background CSV imports (202 + poll + SignalR progress), bulk endpoints with per-item Result envelopes |
| **Announcements & notifications** | Tenant-wide message board; publishing fans out over SignalR (`/hubs/notifications`) to every connected employee — live notification bell in the client |
| **Team chat** | Real-time 1:1 direct messages inside a workspace — SignalR per-user delivery, unread counters, read receipts, conversation sidebar |
| **Self-service signup** | Public landing page registers a company: tenant + TenantAdmin founder account created atomically, auto-login into the fresh workspace |
| **Admin panel** | Workspace account management: provision users, change roles, suspend/reinstate, role → permission matrix |

The Blazor client surfaces all of it: custom-designed landing page with company registration, analytics dashboard (live charts), employees (filters, numbered pagination, hire/edit/terminate, exports), departments, leave requests (submit → approve/reject), attendance (clock-in/out + history), announcements (publish + live feed), team chat (live thread + menu badge), admin panel (users/roles/permissions), CSV imports with live progress, system health, and the anonymous status page.

## Architecture

```
src/
  Core/HrmSystem.Domain                    ← entities, VOs, Result, domain events (zero outward deps)
  Core/HrmSystem.Application               ← CQRS features, behaviors, port interfaces
  Infrastructure/HrmSystem.Infrastructure  ← EF Core 10 + Dapper, Quartz jobs, Identity/JWT, Redis
  Web/HrmSystem.Web                        ← REST controllers, SignalR hubs, health endpoints, OTEL
  Client/HrmSystem.Client                  ← Blazor WASM (REST-only), Vuexy theme, ApexCharts interop
tests/
  ...UnitTests | ...ArchitectureTests (NetArchTest) | ...IntegrationTests (Testcontainers)
```

Layer boundaries are **enforced by architecture tests** (`tests/HrmSystem.ArchitectureTests`), not just convention. See `docs/architecture.md` for decisions.

### Multi-tenancy

- Tenant-owned aggregates derive from `ATenantEntity<TId>`; reads are guarded by **EF Core 10 named query filters** (`SoftDelete` + `Tenant`), writes by `TenantWriteGuardInterceptor` (stamps `TenantId` on insert, throws `CrossTenantWriteException` on mismatch — including mutations reached through owned value objects).
- Dapper bypasses EF filters, so **every tenant-scoped Dapper method takes an explicit `TenantId`** — enforced by a reflection test.
- Isolation is proven against real SQL Server in `tests/HrmSystem.Infrastructure.IntegrationTests` (Testcontainers).

## Run it

### Full stack (Docker)

```powershell
docker compose up -d --build
```

| URL | What |
| --- | --- |
| http://localhost:5100 | Blazor client (nginx, proxies `/api` + `/hubs` to the API — same origin, no CORS) |
| http://localhost:5100/status | Anonymous status page (live SignalR updates) |
| http://localhost:5000/health | API liveness (readiness at `/health/ready`) |
| http://localhost:5000/scalar | API reference (Development) |
| http://localhost:8082 | Seq (logs + OTLP traces) |

### Local development

```powershell
docker compose up -d hrmsystem.sqlserver hrmsystem.redis hrmsystem.seq
dotnet run --project src/Web/HrmSystem.Web        # API on your launch profile / ASPNETCORE_URLS
dotnet run --project src/Client/HrmSystem.Client  # WASM dev server on http://localhost:5217
```

The Development environment enables a CORS policy for the WASM dev server origin and seeds full Bogus demo data.

### Demo accounts (Development seed only)

Password for all: `Seed1234`. Tenant slugs: `acme`, `globex`, `initech`.

| Identifier | Role |
| --- | --- |
| `host-admin` | Platform admin (no tenant — sees zero tenant rows by design) |
| `admin-acme` | Tenant admin |
| `manager-acme` | Manager |
| `employee-acme` | Employee |

## Tests

```powershell
dotnet test                                                  # everything
dotnet test tests/HrmSystem.ArchitectureTests                # layer + tenant-safety conventions
dotnet test tests/HrmSystem.Infrastructure.IntegrationTests  # needs Docker (Testcontainers)
```

## Operations

- **Backups**: Quartz `DatabaseBackupJob` (02:00 UTC daily, compressed + checksummed) into the shared `sqlserver_backups` volume; retention 7 daily / 4 weekly; `POST /api/backups/run` for on-demand. See `docs/disaster-recovery-runbook.md`.
- **Auth hardening**: `/api/auth/*` is rate-limited (10 req/min per client IP, 429 + `Retry-After`).
- **Observability**: OpenTelemetry traces/metrics via OTLP to Seq.

## Reference material

Patterns ported from the reference projects in `Helpers/` (MilestoneStack Clean Architecture, DevHabit REST conventions, Vuexy admin template). QuestPDF runs under the Community license (< $1M USD annual revenue).
