using HrmSystem.Application.Common.Interfaces.Data;
using HrmSystem.Application.Common.Interfaces.Data.Repositories;
using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Employees;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Application.Features.Employees.TerminateEmployee;

internal sealed class TerminateEmployeeCommandHandler(
    IEmployeeRepository employeeRepository,
    IUnitOfWork unitOfWork
) : ICommandHandler<TerminateEmployeeCommand>
{
    private readonly IEmployeeRepository _employeeRepository = employeeRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result> Handle(
        TerminateEmployeeCommand command,
        CancellationToken cancellationToken
    )
    {
        Result<EmployeeId> employeeIdResult = EmployeeId.From(command.EmployeeId);
        if (employeeIdResult.IsFailure)
        {
            return employeeIdResult.Errors.ToList();
        }

        Employee? employee = await _employeeRepository.GetByIdAsync(
            employeeIdResult.Value,
            cancellationToken
        );
        if (employee is null)
        {
            return EmployeeErrors.NotFound;
        }

        Result terminationResult = employee.Terminate(command.TerminatedOn);
        if (terminationResult.IsFailure)
        {
            return terminationResult.Errors.ToList();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
