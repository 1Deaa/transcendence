using HrmSystem.Application.Common.Interfaces.Authentication;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Announcements.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Announcements;

namespace HrmSystem.Application.Features.Announcements.PublishAnnouncement;

/*
    //?     Publishes a tenant-wide announcement. The author display name is denormalized
    //?     from the current user at publish time; the AnnouncementPublishedDomainEvent
    //?     (raised by the factory) fires AFTER SaveChangesAsync and fans out via SignalR.
*/
internal sealed class PublishAnnouncementCommandHandler(
    IAnnouncementRepository announcementRepository,
    ICurrentUserContext currentUserContext,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork
) : ICommandHandler<PublishAnnouncementCommand, AnnouncementResponse>
{
    private readonly IAnnouncementRepository _announcementRepository = announcementRepository;
    private readonly ICurrentUserContext _currentUserContext = currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<AnnouncementResponse>> Handle(
        PublishAnnouncementCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<Announcement> announcementResult = Announcement.Publish(
            command.Title,
            command.Body,
            _currentUserContext.UserName ?? _currentUserContext.Email ?? "System",
            _dateTimeProvider.UtcNow
        );

        if (announcementResult.IsFailure)
        {
            return announcementResult.Errors.ToList();
        }

        await _announcementRepository.AddAsync(announcementResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AnnouncementResponse.FromAnnouncement(announcementResult.Value);
    }
}
