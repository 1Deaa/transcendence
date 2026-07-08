using HrmSystem.Application.Common.Interfaces.Clock;

namespace HrmSystem.Infrastructure.Services.Clock;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
