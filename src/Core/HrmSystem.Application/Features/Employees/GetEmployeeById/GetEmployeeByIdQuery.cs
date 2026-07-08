using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Employees.Shared;

namespace HrmSystem.Application.Features.Employees.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(string EmployeeId) : IQuery<EmployeeResponse>;
