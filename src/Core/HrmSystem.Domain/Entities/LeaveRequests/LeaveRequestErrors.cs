using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

namespace HrmSystem.Domain.Entities.LeaveRequests;

/*
    //?     Error catalog for the LeaveRequest aggregate — one nested class per Value Object.
    //>     Codes: "LeaveRequest.<Reason>" / "LeaveRequest.<ValueObject>.<Reason>" — stable once shipped.
*/
public static class LeaveRequestErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "LeaveRequest.NotFound",
        "The leave request was not found."
    );

    public static readonly Error InvalidLeaveType = Error.Validation(
        "LeaveRequest.InvalidLeaveType",
        "A valid leave type must be specified."
    );

    public static readonly Error AlreadyDecided = Error.Conflict(
        "LeaveRequest.AlreadyDecided",
        "The leave request has already been decided (approved, rejected, or cancelled)."
    );

    public static readonly Error OverlappingLeave = Error.Conflict(
        "LeaveRequest.OverlappingLeave",
        "The employee already has an approved or pending leave overlapping this period."
    );

    public static readonly Error SelfDecisionNotAllowed = Error.Forbidden(
        "LeaveRequest.SelfDecisionNotAllowed",
        "You cannot approve or reject your own leave request — another manager must decide it."
    );

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "LeaveRequest.Id.Invalid",
            "The leave request identifier must be a non-empty string."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "LeaveRequest.Id.InvalidFormat",
            $"The leave request identifier must start with the '{LeaveRequestId.Prefix}' prefix."
        );
    }

    public static class Period
    {
        public static readonly Error EndBeforeStart = Error.Validation(
            "LeaveRequest.Period.EndBeforeStart",
            "The leave end date cannot be earlier than the start date."
        );

        public static readonly Error TooLong = Error.Validation(
            "LeaveRequest.Period.TooLong",
            $"A single leave request cannot exceed {DateRange.MaxDays} days."
        );
    }

    public static class Reason
    {
        public static readonly Error Required = Error.Validation(
            "LeaveRequest.Reason.Required",
            "The leave reason is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "LeaveRequest.Reason.TooLong",
            $"The leave reason cannot exceed {LeaveReason.MaxLength} characters."
        );
    }
}
