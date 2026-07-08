using HrmSystem.Domain.Entities.Imports;

namespace HrmSystem.Application.Features.Imports.Shared;

//? Flat read model for the imports dashboard + polling endpoint.
public sealed record ImportJobResponse(
    string Id,
    string EntityType,
    string FileName,
    string Status,
    int TotalRecords,
    int ProcessedRecords,
    int SuccessfulRecords,
    int FailedRecords,
    IReadOnlyList<string> Errors,
    DateTime CreatedAt,
    DateTime? CompletedAtUtc
)
{
    public static ImportJobResponse FromImportJob(ImportJob job) =>
        new(
            job.Id!.Value,
            job.EntityType.ToString(),
            job.FileName,
            job.Status.ToString(),
            job.TotalRecords,
            job.ProcessedRecords,
            job.SuccessfulRecords,
            job.FailedRecords,
            job.ErrorLog.Length == 0 ? [] : job.ErrorLog.Split('\n'),
            job.CreatedAt,
            job.CompletedAtUtc
        );
}

//? Lightweight live-progress payload pushed over the ImportProgressHub.
public sealed record ImportProgressUpdate(
    string ImportJobId,
    string Status,
    int TotalRecords,
    int ProcessedRecords,
    int SuccessfulRecords,
    int FailedRecords
);
