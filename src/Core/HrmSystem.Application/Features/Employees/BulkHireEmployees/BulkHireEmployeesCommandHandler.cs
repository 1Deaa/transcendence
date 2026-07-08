using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Employees;

namespace HrmSystem.Application.Features.Employees.BulkHireEmployees;

/*
    //?     Batch hire with PER-ITEM Result reporting: every item runs through the same domain
    //?     factories as a single hire; failures collect errors, successes stage inserts.
    //!     ONE SaveChangesAsync at the end — successes commit atomically; a failed item never
    //!     blocks the rest (that is what the per-item report is for).
*/
internal sealed class BulkHireEmployeesCommandHandler(
    IEmployeeRepository employeeRepository,
    IDepartmentRepository departmentRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<BulkHireEmployeesCommand, BulkOperationResult>
{
    public const int MaxItems = 500;

    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<BulkOperationResult>> Handle(
        BulkHireEmployeesCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.Items.Count is 0 or > MaxItems)
        {
            return Error.Validation(
                "Employee.Bulk.InvalidBatchSize",
                $"The batch must contain between 1 and {MaxItems} items."
            );
        }

        //? One-time lookups instead of per-item queries.
        var departmentsById = (
            await _departmentRepository.GetAllAsync(cancellationToken)
        ).ToDictionary(d => d.Id!.Value, StringComparer.Ordinal);

        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var itemResults = new List<BulkItemResult>(command.Items.Count);
        var staged = new List<Employee>();

        for (int index = 0; index < command.Items.Count; index++)
        {
            BulkHireItem item = command.Items[index];
            var itemErrors = new List<BulkItemError>();

            departmentsById.TryGetValue(item.DepartmentId, out Department? department);
            if (department is null)
            {
                itemErrors.Add(
                    new BulkItemError(
                        DepartmentErrors.NotFound.Code,
                        DepartmentErrors.NotFound.Description
                    )
                );
            }

            Result<Employee> hireResult = department is null
                ? DepartmentErrors.NotFound
                : Employee.Hire(
                    item.FirstName,
                    item.LastName,
                    item.Email,
                    item.JobTitle,
                    department.Id!,
                    item.HiredOn
                );

            if (department is not null && hireResult.IsFailure)
            {
                itemErrors.AddRange(
                    hireResult.Errors.Select(e => new BulkItemError(e.Code, e.Description))
                );
            }

            if (itemErrors.Count == 0)
            {
                string email = hireResult.Value.Email.Value;

                //? Duplicate check: within this batch first, then against the tenant's data.
                bool duplicate =
                    !seenEmails.Add(email)
                    || await _employeeRepository.FindByEmailAsync(email, cancellationToken)
                        is not null;

                if (duplicate)
                {
                    Error duplicateError = EmployeeErrors.DuplicateEmail(email);
                    itemErrors.Add(
                        new BulkItemError(duplicateError.Code, duplicateError.Description)
                    );
                }
            }

            if (itemErrors.Count == 0)
            {
                staged.Add(hireResult.Value);
                itemResults.Add(
                    new BulkItemResult(index, hireResult.Value.Id!.Value, true, [])
                );
            }
            else
            {
                itemResults.Add(new BulkItemResult(index, null, false, itemErrors));
            }
        }

        foreach (Employee employee in staged)
        {
            await _employeeRepository.AddAsync(employee, cancellationToken);
        }

        if (staged.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new BulkOperationResult(
            command.Items.Count,
            staged.Count,
            command.Items.Count - staged.Count,
            itemResults
        );
    }
}
