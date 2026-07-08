using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.LeaveRequests.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;

namespace HrmSystem.Application.Features.LeaveRequests.GetLeaveRequests;

internal sealed class GetLeaveRequestsQueryHandler(ILeaveRequestRepository leaveRequestRepository)
    : IQueryHandler<GetLeaveRequestsQuery, PaginationResult<LeaveRequestResponse>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository = leaveRequestRepository;

    public async Task<Result<PaginationResult<LeaveRequestResponse>>> Handle(
        GetLeaveRequestsQuery query,
        CancellationToken cancellationToken
    )
    {
        LeaveStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (
                !Enum.TryParse(query.Status, ignoreCase: true, out LeaveStatus parsed)
                || parsed == LeaveStatus.None
            )
            {
                return Error.Validation(
                    "LeaveRequest.Status.Invalid",
                    $"'{query.Status}' is not a valid leave status."
                );
            }

            status = parsed;
        }

        EmployeeId? employeeId = null;
        if (!string.IsNullOrWhiteSpace(query.EmployeeId))
        {
            Result<EmployeeId> employeeIdResult = EmployeeId.From(query.EmployeeId);
            if (employeeIdResult.IsFailure)
            {
                return employeeIdResult.Errors.ToList();
            }

            employeeId = employeeIdResult.Value;
        }

        (IReadOnlyList<LeaveRequest> items, int totalCount) =
            await _leaveRequestRepository.GetPagedAsync(
                query.Page,
                query.PageSize,
                status,
                employeeId,
                cancellationToken
            );

        var result =
            PaginationResult<LeaveRequestResponse>.Create(
                items.Select(LeaveRequestResponse.FromLeaveRequest).ToList(),
                query.Page,
                query.PageSize,
                totalCount
            );

        return result;
    }
}
