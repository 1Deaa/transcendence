using HrmSystem.Domain.Common.Result;
using MediatR;

namespace HrmSystem.Application.Common.Interfaces.Messaging;

// --- I want to enforce that all the commands in my application return a Result or Result<T> (Implementing Result Pattern)
//      , so that I can have a consistent way of handling success/failure across the application. ---

// Doesn't return any data, just a success/failure result.
public interface ICommand : IRequest<Result>, IBaseCommand;

// Returns data of type TResponse, along with a success/failure result.
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
