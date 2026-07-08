using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.LeaveRequests.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.LeaveRequests;

internal sealed class LeaveRequestIdConverter : ValueConverter<LeaveRequestId, string>
{
    public LeaveRequestIdConverter()
        : base(leaveRequestId => leaveRequestId.Value, rawStr => ConvertToLeaveRequestId(rawStr)) { }

    private static LeaveRequestId ConvertToLeaveRequestId(string rawStr)
    {
        Result<LeaveRequestId> result = LeaveRequestId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted LeaveRequestId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
