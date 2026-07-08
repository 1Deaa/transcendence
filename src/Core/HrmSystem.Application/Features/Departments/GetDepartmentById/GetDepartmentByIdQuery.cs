using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Departments.Shared;

namespace HrmSystem.Application.Features.Departments.GetDepartmentById;

public sealed record GetDepartmentByIdQuery(string DepartmentId) : IQuery<DepartmentResponse>;
