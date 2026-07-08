using HrmSystem.Domain.Entities.Users.ValueObjects;

namespace HrmSystem.Application.Common.Interfaces.Emailing;

public interface IEmailService
{
    Task SendAsync(Email to, string subject, string body, CancellationToken ct);
}
