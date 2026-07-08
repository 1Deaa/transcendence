using FluentValidation;

namespace HrmSystem.Application.Features.Employees.GetAllEmployees;

internal sealed class GetAllEmployeesQueryValidator : AbstractValidator<GetAllEmployeesQuery>
{
    public GetAllEmployeesQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);

        //! Caps the page size so a single request cannot dump an entire tenant's table.
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
