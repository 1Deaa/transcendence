namespace HrmSystem.Domain.Common.Result.Errors;

/*
 //? Immutable, value-typed description of a single domain error.
 *
 //? Designed as a [readonly record struct] so that:
 //?  - Equality is structural (Code + Description + Type), not referential.
 //?  - Instances are stack-allocated — no heap pressure on the hot path.
 //?  - The [with] expression can produce cheap variants without mutation.
 //
 //! The constructor is [private] — all errors must be created via the typed factory methods
 //!  (Failure, Validation, NotFound, …) so every error always carries a meaningful ErrorType.
 //
 //> Convention: define errors in a static class per aggregate:
 //>   public static class UserErrors {
 //>       public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user was not found.");
 //>       public static Error DuplicateEmail(string e) => Error.Conflict("User.DuplicateEmail", $"'{e}' taken.");
 //>   }
 */
public readonly record struct Error
{
    //* Returned by Result.FirstError when the result is successful — safe non-null sentinel.
    //! Never pass None to a failure factory — the invariant check will throw.
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    //* Emitted by Result.Create<TValue> when a null value is supplied where a non-null is required.
    public static readonly Error NullValue = new(
        "Error.NullValue",
        "A null or missing value was provided where a non-null value is required.",
        ErrorType.Failure
    );

    #region Properties

    //? Stable, dot-namespaced machine identifier e.g. "User.NotFound".
    //! Convention: "Aggregate.PascalCaseReason" — never localise or change a code after it ships.
    public string Code { get; }

    //? Human-readable, English-language explanation of what went wrong.
    public string Description { get; }

    //? Classifies the error so the presentation layer can map it to the correct HTTP status code
    //?  or log level without reading the Code string.
    public ErrorType Type { get; }

    #endregion Properties


    #region Constructor — private; use factory methods below

    private Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    #endregion Constructor — private; use factory methods below


    #region Factory methods — one per ErrorType for a fluent, self-documenting API

    //? Creates a general-purpose failure error (no finer category applies).
    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    //? Creates a validation error — input failed one or more business rules.
    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    //? Creates a not-found error — the requested resource does not exist.
    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    //? Creates a conflict error — duplicate key, stale write, state violation, etc.
    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    //? Creates an unauthorized error — no valid identity (authentication required).
    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    //? Creates a forbidden error — authenticated but lacks the required permissions.
    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    //? Creates an unexpected error — bugs or infrastructure faults that alert on-call.
    public static Error Unexpected(string code, string description) =>
        new(code, description, ErrorType.Unexpected);

    //? Creates an unavailable error — a required dependency (DB, cache, scheduler) is down; retry later.
    public static Error Unavailable(string code, string description) =>
        new(code, description, ErrorType.Unavailable);

    #endregion Factory methods — one per ErrorType for a fluent, self-documenting API


    #region Diagnostics

    public override string ToString() => $"[{Type}] {Code}: {Description}";

    #endregion Diagnostics
}
