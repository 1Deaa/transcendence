using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Imports.Enums;
using HrmSystem.Domain.Entities.Imports.ValueObjects;

namespace HrmSystem.Domain.Entities.Imports;

/*
    //?     A background CSV-import job (DevHabit EntryImportJob pattern). TENANT-OWNED.
    //?     Lifecycle: Pending → Processing → Completed | Failed. The uploaded file is stored
    //?     IN the row (varbinary) so the Quartz worker needs no shared filesystem.
    //
    //!     Row-level validation failures do NOT fail the job — they land in ErrorLog
    //!     (newline-joined, capped at MaxLoggedErrors) and the counters tell the story.
    //!     Failed is reserved for fatal conditions (unreadable file, crash).
*/
public class ImportJob : ATenantEntity<ImportJobId>
{
    #region Constants

    public const int MaxLoggedErrors = 100;

    #endregion

    #region Constructor

    private ImportJob(
        ImportJobId id,
        ImportEntityType entityType,
        string fileName,
        byte[] fileContent
    )
        : base(id)
    {
        EntityType = entityType;
        FileName = fileName;
        FileContent = fileContent;
        Status = ImportStatus.Pending;
        ErrorLog = string.Empty;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private ImportJob()
        : base() { }

    #endregion

    #region Properties

    public ImportEntityType EntityType { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    //? The raw uploaded CSV — read once by ProcessImportJob.
    public byte[] FileContent { get; private set; } = [];

    public ImportStatus Status { get; private set; }

    public int TotalRecords { get; private set; }
    public int ProcessedRecords { get; private set; }
    public int SuccessfulRecords { get; private set; }
    public int FailedRecords { get; private set; }

    //? Newline-joined "Row N: message" entries, capped at MaxLoggedErrors.
    public string ErrorLog { get; private set; } = string.Empty;

    public DateTime? CompletedAtUtc { get; private set; }

    //? How many error lines are already logged — cheap cap check without splitting strings.
    public int LoggedErrorCount { get; private set; }

    #endregion

    #region Static factory

    public static Result<ImportJob> Create(
        ImportEntityType entityType,
        string fileName,
        byte[] fileContent
    )
    {
        var errors = new List<Error>();

        if (entityType == ImportEntityType.None)
        {
            errors.Add(ImportJobErrors.InvalidEntityType);
        }

        if (fileContent.Length == 0)
        {
            errors.Add(ImportJobErrors.FileRequired);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new ImportJob(ImportJobId.New(), entityType, fileName, fileContent);
    }

    #endregion

    #region Domain methods

    public Result StartProcessing(int totalRecords)
    {
        if (Status != ImportStatus.Pending)
        {
            return ImportJobErrors.NotPending;
        }

        Status = ImportStatus.Processing;
        TotalRecords = totalRecords;

        return Result.Success();
    }

    //? Called by the worker every progress-save interval (e.g. 100 rows).
    public Result RecordProgress(
        int processedRecords,
        int successfulRecords,
        int failedRecords,
        IReadOnlyList<string> newErrors
    )
    {
        if (Status != ImportStatus.Processing)
        {
            return ImportJobErrors.NotProcessing;
        }

        ProcessedRecords = processedRecords;
        SuccessfulRecords = successfulRecords;
        FailedRecords = failedRecords;

        foreach (string error in newErrors)
        {
            if (LoggedErrorCount >= MaxLoggedErrors)
            {
                break;
            }

            ErrorLog = LoggedErrorCount == 0 ? error : $"{ErrorLog}\n{error}";
            LoggedErrorCount++;
        }

        return Result.Success();
    }

    public Result Complete(DateTime completedAtUtc)
    {
        if (Status != ImportStatus.Processing)
        {
            return ImportJobErrors.NotProcessing;
        }

        Status = ImportStatus.Completed;
        CompletedAtUtc = completedAtUtc;

        //! The file served its purpose — drop the payload so the table doesn't bloat.
        FileContent = [];

        return Result.Success();
    }

    public Result MarkFailed(DateTime completedAtUtc, string fatalError)
    {
        //? Fatal failure can strike in Pending (unreadable file) or Processing (crash).
        if (Status is not (ImportStatus.Pending or ImportStatus.Processing))
        {
            return ImportJobErrors.NotProcessing;
        }

        Status = ImportStatus.Failed;
        CompletedAtUtc = completedAtUtc;
        ErrorLog = LoggedErrorCount == 0 ? fatalError : $"{ErrorLog}\n{fatalError}";
        LoggedErrorCount++;
        FileContent = [];

        return Result.Success();
    }

    #endregion
}
