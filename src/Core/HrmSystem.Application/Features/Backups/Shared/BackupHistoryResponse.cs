using HrmSystem.Domain.Entities.Operations;

namespace HrmSystem.Application.Features.Backups.Shared;

//? Flat read model for the backups dashboard (backups:read).
public sealed record BackupHistoryResponse(
    string Id,
    string FileName,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    long? SizeBytes,
    string? ErrorMessage,
    DateTime? RetainedUntilUtc
)
{
    public static BackupHistoryResponse FromBackupHistory(BackupHistory backup) =>
        new(
            backup.Id!.Value,
            backup.FileName,
            backup.Status.ToString(),
            backup.StartedAtUtc,
            backup.CompletedAtUtc,
            backup.SizeBytes,
            backup.ErrorMessage,
            backup.RetainedUntilUtc
        );
}
