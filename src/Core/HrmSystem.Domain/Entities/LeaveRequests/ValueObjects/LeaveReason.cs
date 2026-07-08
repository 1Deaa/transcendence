using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

/*
    //?     A non-empty, trimmed justification for a leave request (max MaxLength characters).
    //!     The constructor is [private] — use Create() to get a validated instance.
*/
public sealed record LeaveReason
{
    #region Constants

    public const int MaxLength = 500;

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private LeaveReason(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory

    //? Creates a validated LeaveReason — collects all errors before returning.
    public static Result<LeaveReason> Create(string? value)
    {
        var errors = new List<Error>();
        value = value?.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(LeaveRequestErrors.Reason.Required);
        }
        else if (value.Length > MaxLength)
        {
            errors.Add(LeaveRequestErrors.Reason.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new LeaveReason(value!);
    }

    #endregion
}
