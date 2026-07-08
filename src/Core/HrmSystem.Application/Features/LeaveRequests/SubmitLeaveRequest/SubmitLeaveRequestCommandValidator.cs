using FluentValidation;
using HrmSystem.Domain.Entities.LeaveRequests;
using HrmSystem.Domain.Entities.LeaveRequests.Enums;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

namespace HrmSystem.Application.Features.LeaveRequests.SubmitLeaveRequest;

internal sealed class SubmitLeaveRequestCommandValidator
    : AbstractValidator<SubmitLeaveRequestCommand>
{
    public SubmitLeaveRequestCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();

        RuleFor(c => c.Type)
            .NotEqual(LeaveType.None)
            .WithErrorCode(LeaveRequestErrors.InvalidLeaveType.Code)
            .WithMessage(LeaveRequestErrors.InvalidLeaveType.Description);

        RuleFor(c => c.End)
            .GreaterThanOrEqualTo(c => c.Start)
            .WithErrorCode(LeaveRequestErrors.Period.EndBeforeStart.Code)
            .WithMessage(LeaveRequestErrors.Period.EndBeforeStart.Description);

        RuleFor(c => c.Reason)
            .NotEmpty()
            .WithErrorCode(LeaveRequestErrors.Reason.Required.Code)
            .WithMessage(LeaveRequestErrors.Reason.Required.Description)
            .MaximumLength(LeaveReason.MaxLength)
            .WithErrorCode(LeaveRequestErrors.Reason.TooLong.Code)
            .WithMessage(LeaveRequestErrors.Reason.TooLong.Description);
    }
}
