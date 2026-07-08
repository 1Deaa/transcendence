using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Departments.Shared;

namespace HrmSystem.Application.Features.Departments.GetAllDepartments;

public sealed record GetAllDepartmentsQuery : IQuery<IReadOnlyList<DepartmentResponse>>;
