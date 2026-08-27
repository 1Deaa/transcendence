using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.LeaveRequests.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;
using HrmSystem.Application.Common.Authorization;
using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Domain.Entities.Employees;

namespace HrmSystem.Application.Features.LeaveRequests.GetLeaveRequests;

internal sealed class GetLeaveRequestsQueryHandler(
    ILeaveRequestRepository leaveRequestRepository,
    ICurrentUserContext currentUserContext,
    IIdentityService identityService,
    IEmployeeRepository employeeRepository
) : IQueryHandler<GetLeaveRequestsQuery, PaginationResult<LeaveRequestResponse>>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository = leaveRequestRepository;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;
    private readonly IIdentityService _identityService = identityService;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;

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

        string? identityId = _currentUserContext.IdentityId;
        if (identityId is null)
        {
            return Error.Unauthorized("User.Unauthorized", "User is not authenticated.");
        }

        IReadOnlyList<string> permissions = await _identityService.GetPermissionsAsync(identityId, cancellationToken);
        bool canApprove = permissions.Any(p => string.Equals(p, Permissions.Leaves.Approve, StringComparison.OrdinalIgnoreCase));

        if (!canApprove)
        {
            string? email = _currentUserContext.Email;
            if (string.IsNullOrWhiteSpace(email))
            {
                return Error.Forbidden("LeaveRequest.Forbidden", "User does not have an associated employee record.");
            }

            Employee? employee = await _employeeRepository.FindByEmailAsync(email.ToLowerInvariant(), cancellationToken);
            if (employee is null)
            {
                return Error.Forbidden("LeaveRequest.Forbidden", "User does not have an associated employee record.");
            }

            if (employeeId is not null && employeeId != employee.Id)
            {
                return Error.Forbidden("LeaveRequest.Forbidden", "You can only view your own leave requests.");
            }

            employeeId = employee.Id;
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
