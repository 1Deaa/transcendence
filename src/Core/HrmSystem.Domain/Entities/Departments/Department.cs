using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Departments.ValueObjects;

namespace HrmSystem.Domain.Entities.Departments;

/*
    //?     The Department aggregate root — TENANT-OWNED (ATenantEntity → tenant query filter + write guard).
    //?     Rich Domain Model: private ctor + static factory, private setters, named transitions.
*/
public class Department : ATenantEntity<DepartmentId>
{
    #region Constructor

    private Department(DepartmentId id, DepartmentName name, DepartmentCode code)
        : base(id)
    {
        Name = name;
        Code = code;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private Department()
        : base() { }

    #endregion

    #region Properties

    //? Trimmed, non-empty display name. Max DepartmentName.MaxLength characters.
    public DepartmentName Name { get; private set; } = null!;

    //? Short uppercase report/payroll code ("HR", "ENG-2"). Unique per tenant.
    public DepartmentCode Code { get; private set; } = null!;

    #endregion

    #region Static factory

    //? The only way to create a valid Department — collects every broken rule before returning.
    public static Result<Department> Create(string name, string code)
    {
        var errors = new List<Error>();

        Result<DepartmentName> nameResult = DepartmentName.Create(name);
        if (nameResult.IsFailure)
        {
            errors.AddRange(nameResult.Errors);
        }

        Result<DepartmentCode> codeResult = DepartmentCode.Create(code);
        if (codeResult.IsFailure)
        {
            errors.AddRange(codeResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new Department(DepartmentId.New(), nameResult.Value, codeResult.Value);
    }

    #endregion

    #region Domain methods

    //? Updates name and/or code in one validated pass — collects all errors before mutating.
    public Result Update(string name, string code)
    {
        var errors = new List<Error>();

        Result<DepartmentName> nameResult = DepartmentName.Create(name);
        if (nameResult.IsFailure)
        {
            errors.AddRange(nameResult.Errors);
        }

        Result<DepartmentCode> codeResult = DepartmentCode.Create(code);
        if (codeResult.IsFailure)
        {
            errors.AddRange(codeResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        Name = nameResult.Value;
        Code = codeResult.Value;

        return Result.Success();
    }

    #endregion
}
