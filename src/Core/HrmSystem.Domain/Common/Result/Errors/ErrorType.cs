namespace HrmSystem.Domain.Common.Result.Errors;

/*
     //*     Classifies the nature of an Error so infrastructure layers (API controllers, middleware, mappers)
     //*      can translate it to the correct HTTP status code, log level, or alert without coupling
     //*      domain code to transport concerns.
     //*
     //*     Rule of thumb for HTTP mapping:
     //*       Failure     → 400 Bad Request (generic / default)
     //*       Validation  → 422 Unprocessable Entity
     //*       NotFound    → 404 Not Found
     //*       Conflict    → 409 Conflict
     //*       Unauthorized→ 401 Unauthorized
     //*       Forbidden   → 403 Forbidden
     //*       Unexpected  → 500 Internal Server Error
     //*       Unavailable → 503 Service Unavailable
 */
public enum ErrorType
{
    //? General, unclassified failure — use when no finer category applies.
    Failure = 0,

    //? One or more inputs failed business-rule or format validation.
    Validation = 1,

    //? The requested resource does not exist.
    NotFound = 2,

    //? The operation would violate a uniqueness constraint or state invariant.
    Conflict = 3,

    //? The caller is not authenticated (no valid identity).
    Unauthorized = 4,

    //? The caller is authenticated but lacks the required permissions.
    Forbidden = 5,

    //? An unexpected condition occurred that should alert on-call (bugs, infra faults).
    Unexpected = 6,

    //? A dependency the operation needs (database, cache, scheduler, …) is currently down or unreachable.
    //! Added for the health-check/status module — maps to 503 so clients know to retry later.
    Unavailable = 7,
}
