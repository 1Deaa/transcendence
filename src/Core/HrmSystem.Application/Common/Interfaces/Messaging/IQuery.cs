using HrmSystem.Domain.Common.Result;
using MediatR;

namespace HrmSystem.Application.Common.Interfaces.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>, IBaseQuery;
