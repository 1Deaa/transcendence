using HrmSystem.Domain.Common.Result;
using MediatR;

namespace HrmSystem.Application.Common.Interfaces.Messaging;

internal interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{ }
