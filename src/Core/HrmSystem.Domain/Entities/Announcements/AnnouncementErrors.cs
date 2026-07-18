using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Entities.Announcements;

/*
    //?     Error catalog for the Announcement aggregate — nested classes per value/field
    //?     (same convention as DepartmentErrors / EmployeeErrors).
*/
public static class AnnouncementErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Announcement.NotFound",
        "The announcement was not found."
    );

    public static class Id
    {
        public static readonly Error Invalid = Error.Validation(
            "Announcement.Id.Invalid",
            "The announcement identifier is required."
        );

        public static readonly Error InvalidFormat = Error.Validation(
            "Announcement.Id.InvalidFormat",
            $"The announcement identifier must start with the '{ValueObjects.AnnouncementId.Prefix}' prefix."
        );
    }

    public static class Title
    {
        public const int MaxLength = 150;

        public static readonly Error Required = Error.Validation(
            "Announcement.Title.Required",
            "The announcement title is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "Announcement.Title.TooLong",
            $"The announcement title cannot exceed {MaxLength} characters."
        );
    }

    public static class Body
    {
        public const int MaxLength = 4000;

        public static readonly Error Required = Error.Validation(
            "Announcement.Body.Required",
            "The announcement body is required."
        );

        public static readonly Error TooLong = Error.Validation(
            "Announcement.Body.TooLong",
            $"The announcement body cannot exceed {MaxLength} characters."
        );
    }
}
