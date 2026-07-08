using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

namespace HrmSystem.Application.Features.LeaveRequests.ApproveLeaveRequest;

internal sealed class ApproveLeaveRequestCommandHandler(
    ILeaveRequestRepository leaveRequestRepository,
    ICurrentUserContext currentUserContext,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork
) : ICommandHandler<ApproveLeaveRequestCommand>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository = leaveRequestRepository;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(
        ApproveLeaveRequestCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<LeaveRequestId> idResult = LeaveRequestId.From(command.LeaveRequestId);
        if (idResult.IsFailure)
        {
            return idResult.Errors.ToList();
        }

        LeaveRequest? leaveRequest = await _leaveRequestRepository.GetByIdAsync(
            idResult.Value,
            cancellationToken
        );
        if (leaveRequest is null)
        {
            return LeaveRequestErrors.NotFound;
        }

        //? DecidedBy records the approving manager's domain UserId for the audit trail.
        string decidedBy = _currentUserContext.DomainUserId?.Value ?? "system";

        Result approveResult = leaveRequest.Approve(_dateTimeProvider.UtcNow, decidedBy);
        if (approveResult.IsFailure)
        {
            return approveResult.Errors.ToList();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
