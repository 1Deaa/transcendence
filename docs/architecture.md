# Architecture

HrmSystem is a multi-tenant HR SaaS built with **Clean Architecture**: dependencies
point inward, and the boundaries are enforced by architecture tests (NetArchTest), not
just by convention.

```
src/
  Core/HrmSystem.Domain                    ← entities, value objects, Result pattern, domain events (zero outward deps)
  Core/HrmSystem.Application               ← CQRS features (MediatR), behaviors, port interfaces, validators
  Infrastructure/HrmSystem.Infrastructure  ← EF Core + Dapper, Quartz jobs, Identity/JWT, Redis, backups, reporting
  Web/HrmSystem.Web                        ← REST controllers, SignalR hubs, health endpoints, OpenTelemetry
  Client/HrmSystem.Client                  ← Blazor WebAssembly SPA (REST + SignalR only)
tests/
  ...UnitTests | ...ArchitectureTests (NetArchTest) | ...IntegrationTests (Testcontainers)
```

## Request flow

Browser → nginx (TLS termination, single origin) → ASP.NET Core API:

1. Authentication middleware validates the JWT (bearer header for REST; `?access_token`
   query string on `/hubs` WebSocket handshakes, wired via `JwtBearerOptionsSetup`).
2. The tenant resolution middleware maps the `tenant_id` claim to `ITenantContext`.
3. Authorization runs through a permission-based policy provider (`[HasPermission]`),
   with a blocked-account veto for suspended/deleted users.
4. MediatR dispatches the command/query through pipeline behaviors
   (validation → logging → handler). Handlers return the domain `Result<T>` pattern;
   controllers translate results to RFC 9457 problem responses.

## Multi-tenancy (the module of choice)

Isolation is enforced in four layers and proven by integration tests against a real
SQL Server container (Testcontainers):

- **Reads:** EF Core global query filters — a `Tenant` filter (`entity.TenantId ==
  CurrentTenantId`) plus a `SoftDelete` filter — applied to every tenant-scoped query.
- **Writes:** a `TenantWriteGuardInterceptor` stamps `TenantId` on insert and throws
  `CrossTenantWriteException` on any cross-tenant update/delete.
- **Aggregates:** every aggregate root carries `TenantId`; value objects are owned or
  converter-mapped so they cannot silently detach from their tenant scope.
- **Dapper read models:** bypass EF filters by design, so an architecture test
  (reflection-based) forces every Dapper query method to accept an explicit `TenantId`
  and filter on it.

## Persistence

- **EF Core (writes)** — change tracking, interceptors (audit stamping, tenant guard),
  owned value objects, soft deletes via domain methods (`MarkAsDeleted()`), named
  global query filters.
- **Dapper (reads)** — lean read models for analytics and list pages, always
  tenant-scoped.
- **SQL Server 2022** — relational integrity for a data-model-heavy HR domain;
  `sqlserver_backups` volume shared with the API for backup metadata/DR tooling.

## Real-time (SignalR)

Four hubs: `/hubs/notifications` (bell + announcements), `/hubs/analytics` (dashboard
refresh), `/hubs/imports` (CSV import progress) and `/hubs/status` (anonymous status
page). Groups are tenant-scoped. Browser WebSocket handshakes authenticate with the JWT
passed as a query string (`OnMessageReceived` in `JwtBearerOptionsSetup`); the client
(`HubConnectionFactory`) refreshes tokens through `AuthService` so long-lived
connections survive access-token rotation.

## Frontend

Blazor WebAssembly SPA, REST-only against the API (same origin via nginx — no CORS).
`JwtAuthenticationStateProvider` parses the JWT client-side to drive `AuthorizeView` /
`AuthorizeRouteView` (UX only — the API re-checks every permission server-side).

## Why these choices

- **One language across the stack (C#)** — shared models, one toolchain, one debugger.
- **Clean Architecture + CQRS** — the HR domain is the product; infrastructure is
  swappable and the layer boundaries are test-enforced.
- **EF + Dapper split** — each tool where it is strongest: correctness for writes,
  speed and shape for reads.
- **Result pattern over exceptions** — business failures are values, not control flow
  interrupts; exceptions stay exceptional (and map to 500s).
