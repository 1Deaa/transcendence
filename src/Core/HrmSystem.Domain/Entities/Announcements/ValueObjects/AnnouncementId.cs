using HrmSystem.Domain.Common.Result;

namespace HrmSystem.Domain.Entities.Announcements.ValueObjects;

/*
    //?     Strongly-typed, lexicographically-sortable identity of an Announcement aggregate.
    //?     Format: "ann-{UUIDv7}" — New() always valid; From() validates untrusted input.
    //!     The constructor is [private] — use New() or From() to get an instance.
*/
public sealed record AnnouncementId
{
    #region Constants

    public const string Prefix = "ann-";

    #endregion

    #region Properties

    public string Value { get; }

    #endregion

    #region Constructor

    private AnnouncementId(string value)
    {
        Value = value;
    }

    #endregion

    #region Factories

    public static AnnouncementId New() =>
        new(string.Concat(Prefix, Guid.CreateVersion7().ToString()));

    public static Result<AnnouncementId> From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AnnouncementErrors.Id.Invalid;
        }

        if (!value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return AnnouncementErrors.Id.InvalidFormat;
        }

        return new AnnouncementId(value);
    }

    #endregion

    #region Overrides

    public override string ToString() => Value;

    #endregion
}
