namespace HrmSystem.Domain.Entities.Employees.Enums;

public enum EmployeeStatus
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    //? Currently employed and working.
    Active = 1,

    //? Employed but on an approved leave of absence.
    OnLeave = 2,

    //? Employment ended — record retained for history/payroll.
    Terminated = 3,
}
