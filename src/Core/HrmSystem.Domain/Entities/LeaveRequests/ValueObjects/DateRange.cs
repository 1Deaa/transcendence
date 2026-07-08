using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;

/*
    //?     An inclusive start–end day range (Start ≤ End, max MaxDays days).
    //?     Used for leave periods; TotalDays counts both endpoints (1-day leave → 1).
*/
public sealed record DateRange
{
    #region Constants

    public const int MaxDays = 90;

    #endregion

    #region Properties

    public DateOnly Start { get; }
    public DateOnly End { get; }

    //? Inclusive day count — not persisted, computed on read.
    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    #endregion

    #region Constructor

    private DateRange(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    #endregion

    #region Factory

    //? Creates a validated DateRange — collects all errors before returning.
    public static Result<DateRange> Create(DateOnly start, DateOnly end)
    {
        var errors = new List<Error>();

        if (end < start)
        {
            errors.Add(LeaveRequestErrors.Period.EndBeforeStart);
        }
        else if (end.DayNumber - start.DayNumber + 1 > MaxDays)
        {
            errors.Add(LeaveRequestErrors.Period.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new DateRange(start, end);
    }

    #endregion
}
