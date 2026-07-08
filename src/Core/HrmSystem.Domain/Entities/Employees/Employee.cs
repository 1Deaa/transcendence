using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Entities.Employees.Enums;
using HrmSystem.Domain.Entities.Employees.Events;
using HrmSystem.Domain.Entities.Employees.ValueObjects;

namespace HrmSystem.Domain.Entities.Employees;

/*
    //?     The Employee aggregate root — TENANT-OWNED (ATenantEntity → tenant query filter + write guard).
    //?     Rich Domain Model: private ctor + static factory, private setters, every state
    //?     transition is a named method that enforces its own invariants and raises events.
*/
public class Employee : ATenantEntity<EmployeeId>
{
    #region Constructor

    private Employee(
        EmployeeId id,
        PersonName name,
        Email email,
        JobTitle jobTitle,
        DepartmentId departmentId,
        DateOnly hiredOn
    )
        : base(id)
    {
        Name = name;
        Email = email;
        JobTitle = jobTitle;
        DepartmentId = departmentId;
        HiredOn = hiredOn;
        Status = EmployeeStatus.Active;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private Employee()
        : base() { }

    #endregion

    #region Properties

    public PersonName Name { get; private set; } = null!;

    //? Lowercased work email. Unique per tenant.
    public Email Email { get; private set; } = null!;

    public JobTitle JobTitle { get; private set; } = null!;

    public EmployeeStatus Status { get; private set; }

    public DateOnly HiredOn { get; private set; }

    //? Set only by Terminate(); null while employed.
    public DateOnly? TerminatedOn { get; private set; }

    #endregion

    #region Static factory

    //? The only way to hire (create) a valid Employee — collects every broken rule before returning.
    //> Raises EmployeeHiredDomainEvent so analytics/notifications react after the save commits.
    public static Result<Employee> Hire(
        string firstName,
        string lastName,
        string email,
        string jobTitle,
        DepartmentId departmentId,
        DateOnly hiredOn
    )
    {
        var errors = new List<Error>();

        Result<PersonName> nameResult = PersonName.Create(firstName, lastName);
        if (nameResult.IsFailure)
        {
            errors.AddRange(nameResult.Errors);
        }

        Result<Email> emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            errors.AddRange(emailResult.Errors);
        }

        Result<JobTitle> jobTitleResult = JobTitle.Create(jobTitle);
        if (jobTitleResult.IsFailure)
        {
            errors.AddRange(jobTitleResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var employee = new Employee(
            EmployeeId.New(),
            nameResult.Value,
            emailResult.Value,
            jobTitleResult.Value,
            departmentId,
            hiredOn
        );
        employee.RaiseDomainEvent(new EmployeeHiredDomainEvent(employee.Id!));

        return employee;
    }

    #endregion

    #region Domain methods

    //? Ends employment. Records the date, flips status, raises the termination event.
    public Result Terminate(DateOnly terminatedOn)
    {
        if (Status == EmployeeStatus.Terminated)
        {
            return EmployeeErrors.AlreadyTerminated;
        }

        if (terminatedOn < HiredOn)
        {
            return EmployeeErrors.TerminationBeforeHire;
        }

        Status = EmployeeStatus.Terminated;
        TerminatedOn = terminatedOn;
        RaiseDomainEvent(new EmployeeTerminatedDomainEvent(Id!));

        return Result.Success();
    }

    //? Marks the employee as on approved leave (set by the leave-approval flow).
    public Result StartLeave()
    {
        if (Status != EmployeeStatus.Active)
        {
            return EmployeeErrors.NotActive;
        }

        Status = EmployeeStatus.OnLeave;

        return Result.Success();
    }

    //? Returns the employee from leave back to active duty.
    public Result EndLeave()
    {
        if (Status != EmployeeStatus.OnLeave)
        {
            return EmployeeErrors.NotOnLeave;
        }

        Status = EmployeeStatus.Active;

        return Result.Success();
    }

    //? Moves the employee to a different department.
    public Result ChangeDepartment(DepartmentId departmentId)
    {
        if (Status == EmployeeStatus.Terminated)
        {
            return EmployeeErrors.AlreadyTerminated;
        }

        DepartmentId = departmentId;

        return Result.Success();
    }

    //? Updates the mutable profile fields in one validated pass — collects all errors before mutating.
    public Result UpdateProfile(string firstName, string lastName, string email, string jobTitle)
    {
        var errors = new List<Error>();

        Result<PersonName> nameResult = PersonName.Create(firstName, lastName);
        if (nameResult.IsFailure)
        {
            errors.AddRange(nameResult.Errors);
        }

        Result<Email> emailResult = Email.Create(email);
        if (emailResult.IsFailure)
        {
            errors.AddRange(emailResult.Errors);
        }

        Result<JobTitle> jobTitleResult = JobTitle.Create(jobTitle);
        if (jobTitleResult.IsFailure)
        {
            errors.AddRange(jobTitleResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        Name = nameResult.Value;
        Email = emailResult.Value;
        JobTitle = jobTitleResult.Value;

        return Result.Success();
    }

    #endregion

    #region Navigation & Relations

    //! Every Employee belongs to exactly one Department. (1 Department : M Employees)
    public DepartmentId DepartmentId { get; private set; } = null!;
    public Department Department { get; private set; } = null!;

    #endregion
}
