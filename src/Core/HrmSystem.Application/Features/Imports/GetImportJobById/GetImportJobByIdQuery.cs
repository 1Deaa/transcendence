using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Application.Features.Imports.Shared;

namespace HrmSystem.Application.Features.Imports.GetImportJobById;

public sealed record GetImportJobByIdQuery(string ImportJobId) : IQuery<ImportJobResponse>;
