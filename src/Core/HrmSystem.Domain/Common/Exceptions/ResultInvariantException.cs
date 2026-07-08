using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Common.Exceptions;

//! These are PROGRAMMING ERRORS, not domain errors — they must never reach production.
//! If one surfaces in a test or log, fix the calling code; do not catch and swallow it.
//* Factory methods are [internal] so only this assembly (Domain) can instantiate them,
//*  keeping the exception coupled to the invariant-check site.
public sealed class ResultInvariantException : InvalidOperationException
{
    private ResultInvariantException(string message)
        : base(message) { }

    //? Raised when a Result is constructed as successful but carries one or more errors.
    //? A successful result must have an empty error list — errors belong only on failures.
    internal static ResultInvariantException SuccessWithErrors() =>
        new("Invalid Result state: a successful Result must not carry any errors.");

    //? Raised when a Result is constructed as a failure but carries no errors.
    //? A failed result must have at least one error explaining the cause of the failure.
    internal static ResultInvariantException FailureWithoutErrors() =>
        new("Invalid Result state: a failed Result must carry at least one error.");

    //! Raised when Result<TValue>.Value is accessed on a failed result.
    //* Guard with IsSuccess before accessing Value, or use TryGetValue to avoid the throw entirely.
    internal static ResultInvariantException FailureValueAccessed(Error firstError) =>
        new($"Cannot access Value on a failed Result. First error → {firstError}");
}
