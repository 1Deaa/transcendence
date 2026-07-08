using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.Enums;
using HrmSystem.Domain.Entities.Employees.ValueObjects;
using HrmSystem.Domain.Entities.LeaveRequests;

namespace HrmSystem.Application.Features.LeaveRequests.SubmitLeaveRequest;

internal sealed class SubmitLeaveRequestCommandHandler(
    ILeaveRequestRepository leaveRequestRepository,
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<SubmitLeaveRequestCommand, string>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository = leaveRequestRepository;
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<string>> Handle(
        SubmitLeaveRequestCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<EmployeeId> employeeIdResult = EmployeeId.From(command.EmployeeId);
        if (employeeIdResult.IsFailure)
        {
            return employeeIdResult.Errors.ToList();
        }

        Employee? employee = await _employeeRepository.GetByIdAsync(
            employeeIdResult.Value,
            cancellationToken
        );
        if (employee is null)
        {
            return EmployeeErrors.NotFound;
        }

        if (employee.Status == EmployeeStatus.Terminated)
        {
            return EmployeeErrors.AlreadyTerminated;
        }

        //! Overlap guard — one employee cannot stack Pending/Approved leaves on the same days.
        bool overlaps = await _leaveRequestRepository.HasOverlappingAsync(
            employeeIdResult.Value,
            command.Start,
            command.End,
            cancellationToken
        );
        if (overlaps)
        {
            return LeaveRequestErrors.OverlappingLeave;
        }

        Result<LeaveRequest> leaveRequestResult = LeaveRequest.Submit(
            employeeIdResult.Value,
            command.Type,
            command.Start,
            command.End,
            command.Reason
        );
        if (leaveRequestResult.IsFailure)
        {
            return leaveRequestResult.Errors.ToList();
        }

        await _leaveRequestRepository.AddAsync(leaveRequestResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return leaveRequestResult.Value.Id!.Value;
    }
}
