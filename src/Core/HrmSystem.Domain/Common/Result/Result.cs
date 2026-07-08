using System.Diagnostics.CodeAnalysis;
using HrmSystem.Domain.Common.Exceptions;
using HrmSystem.Domain.Common.Result.Errors;

namespace HrmSystem.Domain.Common.Result;

#region Result (non-generic) — commands / void operations that produce no value on success

/*
    //?     Represents the outcome of an operation that produces no value on success.
    //
    //?     Design principles:
    //?       1. [protected internal] constructors enforce all instances are created via static
    //?          factory methods — the same encapsulation applied to domain entities.
    //?          External assemblies (Application, Infrastructure, Web) cannot call [new Result(…)].
    //?       2. Errors are stored in an immutable array so a single failure can carry multiple
    //?          validation errors — no string concatenation, no "first error wins" compromise.
    //?       3. All factory methods, including generic overloads, live on this class so callers
    //?          write [Result.Success(user)] and never need to import the generic namespace.
    //?       4. [implicit operators] let you return an [Error], [List<Error>], or [Error[]] directly
    //?          from a method that returns [Result], keeping call sites clean and boilerplate-free.
*/
public class Result
{
    #region Constructor

    /*
        //?     [protected internal] is a dual access modifier:
        //?       - [protected]  → Result<TValue> (derived class) can call it.
        //?       - [internal]   → the same assembly (HrmSystem.Domain) can call it for testing.
        //!     External assemblies (Application, Infrastructure, Web) CANNOT construct a
        //!     Result directly; they MUST use the static factory methods.
        //>     This is the same encapsulation principle as [private constructors] on entities.
    */
    protected internal Result(bool isSuccess, Error[] errors)
    {
        if (isSuccess && errors.Length > 0)
        {
            throw ResultInvariantException.SuccessWithErrors();
        }

        if (!isSuccess && errors.Length == 0)
        {
            throw ResultInvariantException.FailureWithoutErrors();
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    #endregion

    #region Properties

    //? Returns true when the operation completed without errors.
    public bool IsSuccess { get; }

    //? Returns true when the operation failed with at least one error.
    public bool IsFailure => !IsSuccess;

    /*
        //?     The complete, ordered array of errors describing why the operation failed.
        //?     Returns an empty array for a successful result — never null.
        //
        //!     Exposed as [Error[]] rather than [IReadOnlyList<Error>] for two reasons:
        //!       1. C# forbids implicit operators to/from interfaces (CS0552), so [IReadOnlyList]
        //!          would prevent [return result.Errors] from compiling in callers.
        //!       2. [Error] is a [readonly record struct] (value type) — any index-read gives
        //!          a copy of the value, so reads are safe. Index-write would mutate the internal
        //!          array, but that is an accepted trade-off: callers are not expected to mutate errors.
        //
        //>     For validation flows, iterate ALL errors and map them to the response;
        //>     never assume only the first error matters (that is the FirstError use-case).
    */
    public Error[] Errors { get; }

    /*
        //?     The first error in the array, or [Error.None] when the result is successful.
        //>     Use when only one error is expected (not-found, conflict, …).
        //>     For validation flows that may carry multiple errors, iterate [Errors] instead.
    */
    public Error FirstError => Errors.Length > 0 ? Errors[0] : Error.None;

    #endregion

    #region Polymorphic value inspection

    /*
        //?     When this instance is a successful Result<TValue>, returns true and assigns
        //?     the boxed value to [value].
        //
        //>     Exists for polymorphic use-cases (e.g., MediatR pipeline behaviors) where the
        //>     concrete result type is not known at compile time.
        //>     Prefer the type-safe [Result<TValue>.TryGetValue] whenever possible.
        //
        //!     Plain Result (non-generic) never carries a payload, so this base implementation
        //!     always returns false. The override in Result<TValue> is the authoritative one.
    */
    public virtual bool TryGetSuccessValue(out object? value)
    {
        value = null;
        return false;
    }

    #endregion

    #region Methods

    /*
        //?     Projects the result to a value of [TNext].
        //?     Executes [onSuccess] when the result succeeded,
        //?     or [onFailure] with the full error array when it failed.
        //>     Use this to collapse a Result into a response DTO or HTTP result
        //>     without an explicit if/else block.
    */
    public TNext Match<TNext>(Func<TNext> onSuccess, Func<Error[], TNext> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Errors);

    public Task<TNext> MatchAsync<TNext>(
        Func<Task<TNext>> onSuccess,
        Func<Error[], Task<TNext>> onFailure
    ) => IsSuccess ? onSuccess() : onFailure(Errors);

    //? Executes action only when the result is successful. Returns this for fluent chaining.
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }

        return this;
    }

    //? Executes action only when the result is a failure. Returns this for fluent chaining.
    public Result OnFailure(Action<Error[]> action)
    {
        if (IsFailure)
        {
            action(Errors);
        }

        return this;
    }

    #endregion

    #region Static factory methods

    #region Non-generic (void / command) results

    //? Creates a successful Result — no value, no errors.
    public static Result Success() => new(true, []);

    //? Creates a failed Result carrying a single error.
    public static Result Failure(Error error) => new(false, [error]);

    /*
        //?     Creates a failed Result carrying multiple errors.
        //>     Primary factory for validation flows where several fields may be invalid simultaneously.
        //>     Each field gets its own Error entry; callers receive the complete list, not a concatenated string.
    */
    public static Result Failure(IReadOnlyList<Error> errors)
    {
        if (errors is null || errors.Count == 0)
        {
            throw new ArgumentException("At least one error must be provided.", nameof(errors));
        }

        return new(false, [.. errors]);
    }

    #endregion

    #region Generic (value-carrying) results

    //? Creates a successful Result<TValue> carrying value.
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, []);

    /*
        //!     Creates a failed Result<TValue> carrying a single error.
        //!     The value field is left at [default] (null for reference types, 0 for value types).
        //!     Accessing Result<TValue>.Value on this instance will throw ResultInvariantException —
        //!     guard with IsSuccess first.
    */
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, [error]);

    /*
        //?     Creates a failed Result<TValue> carrying multiple errors.
        //>     Same multi-error semantics as the non-generic overload — all errors are preserved
        //>     and propagated to the caller as an ordered, immutable array.
    */
    public static Result<TValue> Failure<TValue>(IReadOnlyList<Error> errors)
    {
        if (errors is null || errors.Count == 0)
        {
            throw new ArgumentException("At least one error must be provided.", nameof(errors));
        }

        return new(default, false, [.. errors]);
    }

    /*
        //?     Converts a nullable value to a Result<TValue>:
        //?       - non-null → Success<TValue>(value)
        //?       - null     → Failure<TValue>(Error.NullValue)
        //>     Use at system boundaries (repositories, external APIs) when you receive a value
        //>     that may be null and want to propagate a typed failure rather than return null.
    */
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

    #endregion

    #region Implicit operators

    //* Allows returning an Error directly from a method that returns Result:
    //*   return UserErrors.NotFound;   // instead of: return Result.Failure(error);
    public static implicit operator Result(Error error) => Failure(error);

    //* Allows returning a List<Error> directly — used when a validation step accumulates
    //* multiple errors. The list is copied into the immutable internal array on construction.
    public static implicit operator Result(List<Error> errors) => Failure(errors);

    //* Allows returning [result.Errors] (Error[]) directly when propagating errors across Results.
    //*   return otherResult.Errors;   // instead of: return Result.Failure(otherResult.Errors);
    public static implicit operator Result(Error[] errors) => Failure(errors);

    #endregion

    #endregion
}

#endregion


#region Result<TValue> (generic) — queries / operations that return data on success

/*
    //?     Represents the outcome of an operation that produces a TValue on success,
    //?     or one-or-more Error instances on failure.
    //
    //?     Design principles:
    //?       - Inherits the dual-access-modifier constructor pattern and all factory methods
    //?         from Result — callers use [Result.Success(value)], not the constructor.
    //?       - Value guards access: reading it on a failure throws rather than returning a
    //?         misleading default. Use TryGetValue to avoid the throw.
    //?       - The functional combinators (Match, Map, Bind and their async variants) let you
    //?         transform and chain results without if/else ladders or repeated IsSuccess checks.
    //?       - [implicit operator Result<TValue>(TValue?)] lets you write [return value;] instead
    //?         of [return Result.Success(value);] in most scenarios.
*/
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    #region Constructor

    /*
        //?     [protected internal] — the same dual access modifier contract as Result's constructor:
        //?       - [protected]  → further derived classes (if any) can chain this constructor.
        //?       - [internal]   → the Domain assembly can call it directly (e.g., from tests).
        //!     External assemblies MUST use the static factory methods on Result.
    */
    protected internal Result(TValue? value, bool isSuccess, Error[] errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    #endregion

    #region Properties

    /*
        //?     The value produced by a successful operation.
        //
        //!     Accessing this on a failed result throws ResultInvariantException.
        //!     Always guard with IsSuccess or use TryGetValue to avoid the exception entirely.
        //
        //>     The [NotNull] attribute tells the compiler this property never returns null when
        //>     called on a successful result — because we throw instead of returning null on failure.
    */
    [NotNull]
    public TValue Value =>
        IsSuccess ? _value! : throw ResultInvariantException.FailureValueAccessed(FirstError);

    #endregion

    #region Type-safe value inspection

    /*
        //?     Type-safe, exception-free alternative to accessing Value directly.
        //?     Returns true and sets value when the result is successful and the value is non-null;
        //?     otherwise sets value to default.
        //
        //>     Prefer this over [if (result.IsSuccess) use(result.Value)] because it collapses
        //>     the null check and the success check into a single conditional branch.
        //>     This is the type-safe counterpart to the boxing TryGetSuccessValue(out object?)
        //>     inherited from Result. Prefer this in any context where TValue is known at compile time.
    */
    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        if (IsSuccess && _value is not null)
        {
            value = _value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    //* Polymorphic override for MediatR pipeline behaviors. In typed contexts, use TryGetValue.
    public override bool TryGetSuccessValue(out object? value)
    {
        if (IsSuccess && _value is not null)
        {
            value = _value;
            return true;
        }
        else
        {
            value = null;
            return false;
        }
    }

    #endregion

    #region Methods

    /*
        //?     Projects the result to [TNext].
        //?     Executes [onSuccess] with the current value when succeeded,
        //?     or [onFailure] with the full error array when failed.
        //>     Use this to collapse a Result<TValue> into a response DTO, HTTP result, or view model
        //>     without an explicit if/else block.
    */
    public TNext Match<TNext>(
        Func<TValue, TNext> onSuccess,
        Func<Error[], TNext> onFailure
    ) => IsSuccess ? onSuccess(Value) : onFailure(Errors);

    public Task<TNext> MatchAsync<TNext>(
        Func<TValue, Task<TNext>> onSuccess,
        Func<Error[], Task<TNext>> onFailure
    ) => IsSuccess ? onSuccess(Value) : onFailure(Errors);

    /*
        //?     Transforms the success value using [mapper] without touching the error path.
        //?     If the result is a failure, the full error array is propagated into Result<TNext>.
        //>     Functional [map] (Functor) operation. Use it to project a domain entity into a DTO:
        //>       Result<UserDto> dto = result.Map(user => user.ToDto());
    */
    public Result<TNext> Map<TNext>(Func<TValue, TNext> mapper) =>
        IsSuccess ? Success(mapper(Value)) : Failure<TNext>(Errors);

    public async Task<Result<TNext>> MapAsync<TNext>(Func<TValue, Task<TNext>> mapper) =>
        IsSuccess ? Success(await mapper(Value)) : Failure<TNext>(Errors);

    /*
        //?     Chains two failable operations: if this result is successful, passes its value to
        //?     [binder] and returns that result; otherwise propagates the errors.
        //>     Functional [bind] / [flat-map] (Monad) operation. Use to compose sequential steps
        //>     where any step may fail — without nesting:
        //>       Result<Invoice> result =
        //>           GetUser(id)
        //>               .Bind(user => ValidatePermissions(user))
        //>               .Bind(user => CreateInvoice(user, amount));
    */
    public Result<TNext> Bind<TNext>(Func<TValue, Result<TNext>> binder) =>
        IsSuccess ? binder(Value) : Failure<TNext>(Errors);

    public async Task<Result<TNext>> BindAsync<TNext>(Func<TValue, Task<Result<TNext>>> binder) =>
        IsSuccess ? await binder(Value) : Failure<TNext>(Errors);

    #endregion

    #region Implicit operators

    //* Allows returning a TValue directly from a method that returns Result<TValue>,
    //* wrapping it via Result.Create<TValue> — null becomes Failure(Error.NullValue).
    public static implicit operator Result<TValue>(TValue? value) => Create(value);

    //* Allows returning an Error directly from a method that returns Result<TValue>:
    //*   return UserErrors.NotFound;   // instead of: return Result.Failure<TValue>(error);
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    //* Allows returning a List<Error> directly — used when a validation step accumulates
    //* multiple errors before returning.
    public static implicit operator Result<TValue>(List<Error> errors) => Failure<TValue>(errors);

    //* Allows returning [result.Errors] (Error[]) directly when propagating errors across Results.
    //*   return otherResult.Errors;   // instead of: return Result.Failure<TValue>(otherResult.Errors);
    public static implicit operator Result<TValue>(Error[] errors) => Failure<TValue>(errors);

    #endregion
}

#endregion
