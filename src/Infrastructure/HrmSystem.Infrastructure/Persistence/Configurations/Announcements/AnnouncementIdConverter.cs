using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Announcements.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HrmSystem.Infrastructure.Persistence.Configurations.Announcements;

internal sealed class AnnouncementIdConverter : ValueConverter<AnnouncementId, string>
{
    public AnnouncementIdConverter()
        : base(announcementId => announcementId.Value, rawStr => ConvertToAnnouncementId(rawStr)) { }

    private static AnnouncementId ConvertToAnnouncementId(string rawStr)
    {
        Result<AnnouncementId> result = AnnouncementId.From(rawStr);
        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"Database contains a corrupted AnnouncementId: '{rawStr}'."
                    + $" (Error: {result.FirstError.Description})"
            );
        }

        return result.Value;
    }
}
