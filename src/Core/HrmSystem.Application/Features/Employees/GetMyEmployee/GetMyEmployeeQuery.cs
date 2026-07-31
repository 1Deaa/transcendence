using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Employees.Shared;

namespace HrmSystem.Application.Features.Employees.GetMyEmployee;

/*
    //?     Resolves the employee record belonging to the CURRENT user (matched by the
    //?     email claim). Lets Employee-role users identify themselves without the
    //?     employees:read permission — e.g. to submit their own leave request.
*/
public sealed record GetMyEmployeeQuery : IQuery<EmployeeResponse>;
