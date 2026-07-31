namespace HrmSystem.Client.Models;

/*
    //?     Client-side mirrors of the API's response records — same property names so
    //?     System.Net.Http.Json deserializes them without custom converters.
    //!     These are DTO copies, NOT shared types: the WASM client talks to the API over
    //!     REST only and never references server assemblies (plan §2 — enforced by design).
*/

public sealed record AccessTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresOnUtc
);

public sealed record KpiSummary(
    int TotalEmployees,
    int PresentToday,
    int LateToday,
    int AbsentToday,
    int PendingLeaves,
    int Departments
);

public sealed record TimeSeriesPoint(DateOnly Date, decimal Value);

public sealed record ChartSeries(string Name, IReadOnlyList<TimeSeriesPoint> Points);

public sealed record CategoryCount(string Label, int Value);

public sealed record LeaveStats(
    IReadOnlyList<CategoryCount> ByType,
    IReadOnlyList<CategoryCount> ByStatus
);

public sealed record PublicComponentStatus(string Name, string Status);

public sealed record LastBackupSummary(DateTime? CompletedAtUtc, string? Status);

public sealed record PublicStatusResponse(
    string OverallStatus,
    IReadOnlyList<PublicComponentStatus> Components,
    double Uptime30dPercent,
    double Uptime90dPercent,
    DateTime? LastCheckedAtUtc,
    LastBackupSummary? LastBackup
);

public sealed record ComponentHealthResponse(
    string Name,
    string Status,
    double DurationMs,
    string? Description,
    DateTime CheckedAtUtc
);

public sealed record UptimePoint(DateOnly Date, double UptimePercent);

public sealed record DepartmentResponse(
    string Id,
    string Name,
    string Code,
    bool IsActive,
    DateTime CreatedAt
);

public sealed record EmployeeResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    string DepartmentId,
    string Status,
    DateOnly HiredOn,
    DateOnly? TerminatedOn
);

public sealed record PaginationResult<TItem>(
    IReadOnlyList<TItem> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}

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
);

public sealed record ImportProgressUpdate(
    string ImportJobId,
    string Status,
    int TotalRecords,
    int ProcessedRecords,
    int SuccessfulRecords,
    int FailedRecords
);

public sealed record LeaveRequestResponse(
    string Id,
    string EmployeeId,
    string Type,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int TotalDays,
    string Reason,
    string Status,
    DateTime? DecidedAt,
    string? DecidedBy
);

public sealed record AttendanceResponse(
    string Id,
    string EmployeeId,
    DateOnly Date,
    DateTime? ClockInAt,
    DateTime? ClockOutAt,
    string Status,
    int? WorkedMinutes
);

public sealed record AnnouncementResponse(
    string Id,
    string Title,
    string Body,
    string AuthorName,
    DateTime PublishedAtUtc
);

public sealed record ChatMessageResponse(
    string Id,
    string SenderUserId,
    string RecipientUserId,
    string Content,
    DateTime SentAtUtc,
    DateTime? ReadAtUtc
);

public sealed record ConversationSummary(
    string OtherUserId,
    string OtherUserName,
    string OtherFirstName,
    string OtherLastName,
    string LastMessage,
    DateTime LastMessageAtUtc,
    int UnreadCount
);

public sealed record ChatUserSummary(
    string UserId,
    string UserName,
    string FirstName,
    string LastName
);

public sealed record TenantUserSummary(
    string UserId,
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);

public sealed record RoleSummary(string Name, IReadOnlyList<string> Permissions);

//? Problem-details subset — enough to show API validation messages in forms.
//! Validation failures (RFC 9457) carry their messages in [Errors], not [Detail].
public sealed record ApiProblem(
    string? Title,
    string? Detail,
    Dictionary<string, string[]>? Errors
)
{
    //? Best human-readable message: detail → flattened validation errors → title.
    public string? Message()
    {
        if (!string.IsNullOrWhiteSpace(Detail))
        {
            return Detail;
        }

        if (Errors is { Count: > 0 })
        {
            return string.Join(" ", Errors.Values.SelectMany(messages => messages));
        }

        return string.IsNullOrWhiteSpace(Title) ? null : Title;
    }
}
