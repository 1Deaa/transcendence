using FluentValidation;

namespace HrmSystem.Application.Features.Employees.GetAllEmployees;

internal sealed class GetAllEmployeesQueryValidator : AbstractValidator<GetAllEmployeesQuery>
{
    public GetAllEmployeesQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);

        /*
            //!     Upper bound on the payload size of a single request.
            //!     500 (not 100) because the attendance and leave pages load the whole
            //!     tenant roster to fill their employee pickers and resolve names —
            //!     a seeded tenant already holds ~470 employees. The endpoint is
            //!     permission-gated (employees:read) and tenant-filtered, so this bounds
            //!     payload size rather than guarding against untrusted callers.
        */
        RuleFor(q => q.PageSize).InclusiveBetween(1, 500);
    }
}
