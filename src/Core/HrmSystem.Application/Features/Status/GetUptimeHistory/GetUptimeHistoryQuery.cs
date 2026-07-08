using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Status.Shared;

namespace HrmSystem.Application.Features.Status.GetUptimeHistory;

public sealed record GetUptimeHistoryQuery(int Days = 30) : IQuery<IReadOnlyList<UptimePoint>>;
