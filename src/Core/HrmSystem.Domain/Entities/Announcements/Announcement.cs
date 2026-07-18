using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Announcements.Events;
using HrmSystem.Domain.Entities.Announcements.ValueObjects;

namespace HrmSystem.Domain.Entities.Announcements;

/*
    //?     The Announcement aggregate root — TENANT-OWNED (ATenantEntity → tenant query
    //?     filter + write guard). A company-wide message pushed to every employee in the
    //?     workspace in real time (AnnouncementPublishedDomainEvent → NotificationsHub).
    //!     Immutable after publish on purpose: corrections are NEW announcements — an edited
    //!     broadcast that some employees already read is a communication bug, not a feature.
*/
public class Announcement : ATenantEntity<AnnouncementId>
{
    #region Constructor

    private Announcement(
        AnnouncementId id,
        string title,
        string body,
        string authorName,
        DateTime publishedAtUtc
    )
        : base(id)
    {
        Title = title;
        Body = body;
        AuthorName = authorName;
        PublishedAtUtc = publishedAtUtc;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private Announcement()
        : base() { }

    #endregion

    #region Properties

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    //? Display name of the publisher, denormalized at publish time — announcements outlive accounts.
    public string AuthorName { get; private set; } = null!;

    public DateTime PublishedAtUtc { get; private set; }

    #endregion

    #region Static factory

    //? The only way to create a valid Announcement — collects every broken rule before returning.
    public static Result<Announcement> Publish(
        string title,
        string body,
        string authorName,
        DateTime utcNow
    )
    {
        var errors = new List<Error>();

        string trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length == 0)
        {
            errors.Add(AnnouncementErrors.Title.Required);
        }
        else if (trimmedTitle.Length > AnnouncementErrors.Title.MaxLength)
        {
            errors.Add(AnnouncementErrors.Title.TooLong);
        }

        string trimmedBody = body?.Trim() ?? string.Empty;
        if (trimmedBody.Length == 0)
        {
            errors.Add(AnnouncementErrors.Body.Required);
        }
        else if (trimmedBody.Length > AnnouncementErrors.Body.MaxLength)
        {
            errors.Add(AnnouncementErrors.Body.TooLong);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var announcement = new Announcement(
            AnnouncementId.New(),
            trimmedTitle,
            trimmedBody,
            string.IsNullOrWhiteSpace(authorName) ? "System" : authorName.Trim(),
            utcNow
        );

        announcement.RaiseDomainEvent(
            new AnnouncementPublishedDomainEvent(announcement.Id!)
        );

        return announcement;
    }

    #endregion
}
