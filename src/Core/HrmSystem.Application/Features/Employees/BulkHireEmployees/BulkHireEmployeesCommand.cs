using HrmSystem.Application.Common.Interfaces.Messaging;

namespace HrmSystem.Application.Features.Employees.BulkHireEmployees;

public sealed record BulkHireItem(
    string FirstName,
    string LastName,
    string Email,
    string JobTitle,
    string DepartmentId,
    DateOnly HiredOn
);

public sealed record BulkHireEmployeesCommand(IReadOnlyList<BulkHireItem> Items)
    : ICommand<BulkOperationResult>;

//? Per-item Result projection — the whole batch answers 200; each item tells its own story.
public sealed record BulkItemResult(
    int Index,
    string? Id,
    bool Succeeded,
    IReadOnlyList<BulkItemError> Errors
);

public sealed record BulkItemError(string Code, string Description);

public sealed record BulkOperationResult(
    int TotalCount,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<BulkItemResult> Items
);
