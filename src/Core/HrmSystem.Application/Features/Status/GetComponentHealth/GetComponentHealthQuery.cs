using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Status.Shared;

namespace HrmSystem.Application.Features.Status.GetComponentHealth;

//! Authenticated detail view (health:read) — full descriptions and probe durations included.
public sealed record GetComponentHealthQuery : IQuery<IReadOnlyList<ComponentHealthResponse>>;
