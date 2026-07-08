using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Common.Models;
using HrmSystem.Application.Features.Imports.Shared;

namespace HrmSystem.Application.Features.Imports.GetImportJobs;

public sealed record GetImportJobsQuery(int Page = 1, int PageSize = 20)
    : IQuery<PaginationResult<ImportJobResponse>>;
