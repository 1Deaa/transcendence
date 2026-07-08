namespace HrmSystem.Application.Common.Interfaces.Clock;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
