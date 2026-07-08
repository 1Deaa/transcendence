using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Attendances.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Attendances;

internal sealed class AttendanceIdConverter : ValueConverter<AttendanceId, string>
{
    public AttendanceIdConverter()
        : base(attendanceId => attendanceId.Value, rawStr => ConvertToAttendanceId(rawStr)) { }

    private static AttendanceId ConvertToAttendanceId(string rawStr)
    {
        Result<AttendanceId> result = AttendanceId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted AttendanceId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
