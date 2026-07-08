namespace HrmSystem.Domain.Entities.Operations.Enums;

/*
    //?     Domain mirror of Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus —
    //?     the Domain layer must not reference the ASP.NET Core health abstractions package.
    //>     Mapping: Healthy→200-path, Degraded→200 + "Degraded" flag, Unhealthy→503 (ErrorType.Unavailable).
*/
public enum HealthState
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
}
