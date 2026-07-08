using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Employees.BulkHireEmployees;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Employees.BulkDeleteEmployees;

/*
    //?     Batch SOFT delete with per-item Result reporting — same envelope as bulk hire.
    //!     Soft delete via the repository (never a raw Remove) so history survives and the
    //!     query filter hides the rows; one SaveChangesAsync commits all successes together.
*/
internal sealed class BulkDeleteEmployeesCommandHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<BulkDeleteEmployeesCommand, BulkOperationResult>
{
    public const int MaxItems = 500;

    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<BulkOperationResult>> Handle(
        BulkDeleteEmployeesCommand command,
        CancellationToken cancellationToken
    )
    {
        if (command.EmployeeIds.Count is 0 or > MaxItems)
        {
            return Error.Validation(
                "Employee.Bulk.InvalidBatchSize",
                $"The batch must contain between 1 and {MaxItems} items."
            );
        }

        var itemResults = new List<BulkItemResult>(command.EmployeeIds.Count);
        int successCount = 0;

        for (int index = 0; index < command.EmployeeIds.Count; index++)
        {
            string rawId = command.EmployeeIds[index];

            Result<EmployeeId> idResult = EmployeeId.From(rawId);
            if (idResult.IsFailure)
            {
                itemResults.Add(
                    new BulkItemResult(
                        index,
                        rawId,
                        false,
                        idResult.Errors.Select(e => new BulkItemError(e.Code, e.Description)).ToList()
                    )
                );
                continue;
            }

            Employee? employee = await _employeeRepository.GetByIdAsync(
                idResult.Value,
                cancellationToken
            );
            if (employee is null)
            {
                itemResults.Add(
                    new BulkItemResult(
                        index,
                        rawId,
                        false,
                        [new BulkItemError(EmployeeErrors.NotFound.Code, EmployeeErrors.NotFound.Description)]
                    )
                );
                continue;
            }

            await _employeeRepository.DeleteEntityAsync(employee);
            itemResults.Add(new BulkItemResult(index, rawId, true, []));
            successCount++;
        }

        if (successCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new BulkOperationResult(
            command.EmployeeIds.Count,
            successCount,
            command.EmployeeIds.Count - successCount,
            itemResults
        );
    }
}
