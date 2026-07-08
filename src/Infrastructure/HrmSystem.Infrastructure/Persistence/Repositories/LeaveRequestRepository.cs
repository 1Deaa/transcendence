using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace HrmSystem.Infrastructure.Persistence.Repositories;

internal sealed class LeaveRequestRepository
    : ABaseRepository<LeaveRequest, LeaveRequestId>, ILeaveRequestRepository
{
    public LeaveRequestRepository(ApplicationDbContext dbContext)
        : base(dbContext) { }

    public async Task<bool> HasOverlappingAsync(
        EmployeeId employeeId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct
    )
    {
        /*
            //?     Two inclusive ranges [a,b] and [c,d] overlap ⇔ a ≤ d AND c ≤ b.
            //!     Only Pending and Approved requests block — Rejected/Cancelled do not.
        */
        return await DbContext
            .Set<LeaveRequest>()
            .AnyAsync(
                l =>
                    l.EmployeeId == employeeId
                    && (l.Status == LeaveStatus.Pending || l.Status == LeaveStatus.Approved)
                    && l.Period.Start <= end
                    && start <= l.Period.End,
                ct
            );
    }

    public async Task<(IReadOnlyList<LeaveRequest> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        LeaveStatus? status,
        EmployeeId? employeeId,
        CancellationToken ct
    )
    {
        IQueryable<LeaveRequest> query = DbContext.Set<LeaveRequest>().AsNoTracking();

        if (status is not null)
        {
            query = query.Where(l => l.Status == status);
        }

        if (employeeId is not null)
        {
            query = query.Where(l => l.EmployeeId == employeeId);
        }

        int totalCount = await query.CountAsync(ct);

        List<LeaveRequest> items = await query
            .OrderByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
