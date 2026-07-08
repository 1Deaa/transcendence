using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using MediatR;
using IBaseRequest = HrmSystem.Application.Common.Interfaces.Messaging.IBaseRequest;

namespace HrmSystem.Application.Common.Behaviors;

/*
//? Runs every registered FluentValidation IValidator<TRequest> before the handler executes.
//? If no validators are registered for TRequest, the call passes straight through — zero overhead.
//
//> Constrained to IBaseRequest so it covers both commands and queries
//>  without coupling to MediatR's IRequest<T>.
//
//! TResponse is always Result or Result<T> — enforced by ICommand / IQuery hierarchy.
//!  Implicit operators on Result don't apply to generic type parameters, so
//!  CreateFailureResult uses a one-time reflection call to invoke the right factory.
*/
internal sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseRequest
{
    //! The Fluent Validation Library exposes: [IValidator] Interface, so we can inject one or more validators for TRequest. which is a Query or Command.
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        //! 1'st Thing in [ValidationBehavior] is to inject all the validators for TRequest (Command/Query)
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        //! Check if there are any validators for TRequest. If not, call the next delegate (Next Pipeline Behavior OR Command/Query Handler) immediately to avoid unnecessary overhead so we return.
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        //! ValidationContext is a class provided by FluentValidation that encapsulates the object being validated (TRequest in this case) and any additional information needed for validation. We create an instance of ValidationContext<TRequest> using the incoming request.
        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var errors = validationResults
            .Where(validationResult => validationResult.Errors.Count != 0)
            .SelectMany(validationResult => validationResult.Errors)
            .Select(validationFailure =>
                Error.Validation(validationFailure.ErrorCode, validationFailure.ErrorMessage)
            )
            .ToList();

        if (errors.Count == 0)
        {
            return await next(cancellationToken);
        }

        return CreateFailureResult(errors);
    }

    private static TResponse CreateFailureResult(List<Error> errors)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure((IReadOnlyList<Error>)errors);
        }

        //> TResponse is Result<T>: invoke Result.Failure<T>(IReadOnlyList<Error>) via reflection.
        //> This path runs once per failed request — the perf cost is negligible.
        Type valueType = typeof(TResponse).GetGenericArguments()[0];
        MethodInfo failureMethod = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m =>
                m.Name == nameof(Result.Failure)
                && m.IsGenericMethodDefinition
                && m.GetParameters() is [{ ParameterType: var p }]
                && p == typeof(IReadOnlyList<Error>)
            )
            .MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, [(IReadOnlyList<Error>)errors])!;
    }
}
