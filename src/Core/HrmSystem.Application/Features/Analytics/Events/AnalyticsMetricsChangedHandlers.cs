using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Entities.Attendances.Events;
using HrmSystem.Domain.Entities.Employees.Events;
using HrmSystem.Domain.Entities.LeaveRequests.Events;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using MediatR;

namespace HrmSystem.Application.Features.Analytics.Events;

/*
    //?     Domain events → "metrics changed" hints for the live dashboard.
    //?     Published AFTER SaveChangesAsync commits (ApplicationDbContext pipeline), so a
    //?     dashboard that re-fetches on the hint always reads consistent data.
    //
    //!     The tenant comes from the ambient ITenantContext — the event fires inside the
    //!     same scope (HTTP request or job scope) that performed the write. No tenant in
    //!     scope (host-level flows) → nothing to push, silently skip.
    //
    //*     Grouped in one file on purpose: each handler is 3 lines of intent — the metric
    //*     keys a business event invalidates.
*/
internal abstract class AAnalyticsMetricsHandler(
    IAnalyticsNotifier notifier,
    ITenantContext tenantContext
)
{
    protected async Task PublishAsync(IReadOnlyList<string> metricKeys, CancellationToken ct)
    {
        TenantId? tenantId = tenantContext.Current;
        if (tenantId is null)
        {
            return;
        }

        await notifier.PublishMetricsChangedAsync(tenantId, metricKeys, ct);
    }
}

internal sealed class EmployeeClockedInAnalyticsHandler(
    IAnalyticsNotifier notifier,
    ITenantContext tenantContext
) : AAnalyticsMetricsHandler(notifier, tenantContext), INotificationHandler<EmployeeClockedInDomainEvent>
{
    public Task Handle(EmployeeClockedInDomainEvent notification, CancellationToken cancellationToken) =>
        PublishAsync(["summary", "attendance-trend"], cancellationToken);
}

internal sealed class EmployeeClockedOutAnalyticsHandler(
    IAnalyticsNotifier notifier,
    ITenantContext tenantContext
) : AAnalyticsMetricsHandler(notifier, tenantContext), INotificationHandler<EmployeeClockedOutDomainEvent>
{
    public Task Handle(EmployeeClockedOutDomainEvent notification, CancellationToken cancellationToken) =>
        PublishAsync(["attendance-trend"], cancellationToken);
}

internal sealed class EmployeeHiredAnalyticsHandler(
    IAnalyticsNotifier notifier,
    ITenantContext tenantContext
) : AAnalyticsMetricsHandler(notifier, tenantContext), INotificationHandler<EmployeeHiredDomainEvent>
{
    public Task Handle(EmployeeHiredDomainEvent notification, CancellationToken cancellationToken) =>
        PublishAsync(["summary", "headcount-trend", "department-distribution"], cancellationToken);
}

internal sealed class EmployeeTerminatedAnalyticsHandler(
    IAnalyticsNotifier notifier,
    ITenantContext tenantContext
) : AAnalyticsMetricsHandler(notifier, tenantContext), INotificationHandler<EmployeeTerminatedDomainEvent>
{
    public Task Handle(EmployeeTerminatedDomainEvent notification, CancellationToken cancellationToken) =>
        PublishAsync(["summary", "headcount-trend", "department-distribution"], cancellationToken);
}

internal sealed class LeaveRequestSubmittedAnalyticsHandler(
    IAnalyticsNotifier notifier,
    ITenantContext tenantContext
) : AAnalyticsMetricsHandler(notifier, tenantContext), INotificationHandler<LeaveRequestSubmittedDomainEvent>
{
    public Task Handle(LeaveRequestSubmittedDomainEvent notification, CancellationToken cancellationToken) =>
        PublishAsync(["summary", "leave-stats"], cancellationToken);
}

internal sealed class LeaveRequestDecidedAnalyticsHandler(
    IAnalyticsNotifier notifier,
    ITenantContext tenantContext
) : AAnalyticsMetricsHandler(notifier, tenantContext), INotificationHandler<LeaveRequestDecidedDomainEvent>
{
    public Task Handle(LeaveRequestDecidedDomainEvent notification, CancellationToken cancellationToken) =>
        PublishAsync(["summary", "leave-stats"], cancellationToken);
}
