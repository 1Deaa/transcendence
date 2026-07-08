using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using HrmSystem.Application.Common.Interfaces.Clock;
using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.RealTime;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Application.Features.Imports.Shared;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Imports;
using HrmSystem.Domain.Entities.Imports.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace HrmSystem.Infrastructure.BackgroundJobs;

/*
    //?     The employee CSV import worker (DevHabit ProcessEntryImportJob pattern):
    //?      1. Loads the job by id from JobDataMap (FindByIdAsync — bypasses filters; the
    //?         worker has no HTTP tenant context yet).
    //?      2. Opens tenantContext.BeginScope(job.TenantId) so every write is stamped/guarded.
    //?      3. Parses with CsvHelper; per row: department lookup, duplicate-email check
    //?         (in-file AND against the DB), Employee.Hire via the domain factory.
    //?      4. Saves + pushes progress every ProgressSaveInterval rows; errors cap at 100.
    //
    //!     Expected CSV columns: FirstName,LastName,Email,JobTitle,DepartmentCode,HiredOn (yyyy-MM-dd).
    //!     Row-level failures never fail the job — Failed is reserved for fatal conditions.
*/
[DisallowConcurrentExecution]
internal sealed class ProcessImportJob(
    IImportJobRepository importJobRepository,
    ApplicationDbContext db,
    IUnitOfWork unitOfWork,
    ITenantContext tenantContext,
    IImportProgressNotifier progressNotifier,
    IDateTimeProvider dateTimeProvider,
    ILogger<ProcessImportJob> logger
) : IJob
{
    public static readonly JobKey Key = new("process-import", "imports");
    public const string ImportJobIdDataKey = "importJobId";
    private const int ProgressSaveInterval = 100;

    public async Task Execute(IJobExecutionContext context)
    {
        string? importJobIdRaw = context.MergedJobDataMap.GetString(ImportJobIdDataKey);
        Result<ImportJobId> idResult = ImportJobId.From(importJobIdRaw);
        if (idResult.IsFailure)
        {
            logger.LogError("ProcessImportJob started without a valid importJobId.");
            return;
        }

        //! FindByIdAsync ignores query filters — no tenant scope exists yet at this point.
        ImportJob? job = await importJobRepository.FindByIdAsync(
            idResult.Value,
            context.CancellationToken
        );
        if (job is null)
        {
            logger.LogError("Import job '{ImportJobId}' was not found.", importJobIdRaw);
            return;
        }

        //? Everything below runs AS the uploading tenant — write guard + query filters active.
        using IDisposable scope = tenantContext.BeginScope(job.TenantId);

        try
        {
            await ProcessAsync(job, context.CancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Result failResult = job.MarkFailed(dateTimeProvider.UtcNow, exception.Message);
            if (failResult.IsFailure)
            {
                logger.LogError("ImportJob.MarkFailed was rejected — state mismatch.");
            }

            await unitOfWork.SaveChangesAsync(context.CancellationToken);
            await NotifyAsync(job, context.CancellationToken);

            logger.LogError(exception, "Import job '{ImportJobId}' failed.", job.Id!.Value);
        }
    }

    private async Task ProcessAsync(ImportJob job, CancellationToken ct)
    {
        List<EmployeeCsvRow> rows = ParseCsv(job.FileContent);

        Result startResult = job.StartProcessing(rows.Count);
        if (startResult.IsFailure)
        {
            logger.LogWarning(
                "Import job '{ImportJobId}' is not pending — skipping.",
                job.Id!.Value
            );
            return;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await NotifyAsync(job, ct);

        //? One-time lookups instead of per-row queries: departments by code, existing emails.
        var departmentsByCode = (
            await db.Departments.AsNoTracking().ToListAsync(ct)
        ).ToDictionary(d => d.Code.Value, StringComparer.OrdinalIgnoreCase);

        var knownEmails = (
            await db.Employees.AsNoTracking().Select(e => e.Email).ToListAsync(ct)
        )
            .Select(email => email.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int processed = 0;
        int successful = 0;
        int failed = 0;
        var pendingErrors = new List<string>();

        foreach (EmployeeCsvRow row in rows)
        {
            ct.ThrowIfCancellationRequested();
            processed++;
            int rowNumber = processed + 1; //? +1 → human row number including the header line.

            Result<Employee> rowResult = BuildEmployee(row, departmentsByCode, knownEmails, rowNumber, pendingErrors);
            if (rowResult.IsSuccess)
            {
                await db.Employees.AddAsync(rowResult.Value, ct);
                knownEmails.Add(rowResult.Value.Email.Value);
                successful++;
            }
            else
            {
                failed++;
            }

            if (processed % ProgressSaveInterval == 0)
            {
                job.RecordProgress(processed, successful, failed, pendingErrors);
                pendingErrors.Clear();
                await unitOfWork.SaveChangesAsync(ct);
                await NotifyAsync(job, ct);
            }
        }

        job.RecordProgress(processed, successful, failed, pendingErrors);
        Result completeResult = job.Complete(dateTimeProvider.UtcNow);
        if (completeResult.IsFailure)
        {
            logger.LogError("ImportJob.Complete was rejected — state mismatch.");
        }

        await unitOfWork.SaveChangesAsync(ct);
        await NotifyAsync(job, ct);

        logger.LogInformation(
            "Import job '{ImportJobId}' completed: {Successful}/{Total} rows imported, {Failed} failed.",
            job.Id!.Value,
            successful,
            processed,
            failed
        );
    }

    private static Result<Employee> BuildEmployee(
        EmployeeCsvRow row,
        Dictionary<string, Department> departmentsByCode,
        HashSet<string> knownEmails,
        int rowNumber,
        List<string> pendingErrors
    )
    {
        if (
            string.IsNullOrWhiteSpace(row.DepartmentCode)
            || !departmentsByCode.TryGetValue(row.DepartmentCode.Trim(), out Department? department)
        )
        {
            pendingErrors.Add($"Row {rowNumber}: unknown department code '{row.DepartmentCode}'.");
            return DepartmentErrors.NotFound;
        }

        if (
            !DateOnly.TryParse(
                row.HiredOn,
                CultureInfo.InvariantCulture,
                out DateOnly hiredOn
            )
        )
        {
            pendingErrors.Add($"Row {rowNumber}: invalid HiredOn date '{row.HiredOn}' (expected yyyy-MM-dd).");
            return EmployeeErrors.NotFound; //! Never surfaced — the error text above is what matters.
        }

        Result<Employee> employeeResult = Employee.Hire(
            row.FirstName,
            row.LastName,
            row.Email,
            row.JobTitle,
            department.Id!,
            hiredOn
        );
        if (employeeResult.IsFailure)
        {
            pendingErrors.Add(
                $"Row {rowNumber}: {string.Join("; ", employeeResult.Errors.Select(e => e.Description))}"
            );
            return employeeResult;
        }

        if (knownEmails.Contains(employeeResult.Value.Email.Value))
        {
            pendingErrors.Add(
                $"Row {rowNumber}: duplicate email '{employeeResult.Value.Email.Value}'."
            );
            return EmployeeErrors.DuplicateEmail(employeeResult.Value.Email.Value);
        }

        return employeeResult;
    }

    private static List<EmployeeCsvRow> ParseCsv(byte[] fileContent)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            HeaderValidated = null,
        };

        using var memoryStream = new MemoryStream(fileContent);
        using var streamReader = new StreamReader(memoryStream);
        using var csvReader = new CsvReader(streamReader, config);

        return csvReader.GetRecords<EmployeeCsvRow>().ToList();
    }

    private async Task NotifyAsync(ImportJob job, CancellationToken ct)
    {
        var progress = new ImportProgressUpdate(
            job.Id!.Value,
            job.Status.ToString(),
            job.TotalRecords,
            job.ProcessedRecords,
            job.SuccessfulRecords,
            job.FailedRecords
        );

        await progressNotifier.PublishProgressAsync(job.TenantId, progress, ct);
    }

    //? CsvHelper row shape — property names match the expected header columns.
    private sealed class EmployeeCsvRow
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string HiredOn { get; set; } = string.Empty;
    }
}
