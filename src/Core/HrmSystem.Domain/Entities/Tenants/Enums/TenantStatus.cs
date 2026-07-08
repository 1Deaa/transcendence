namespace HrmSystem.Domain.Entities.Tenants.Enums;

public enum TenantStatus
{
    //! Sentinel — never persist; used to catch uninitialized values in validation.
    None = 0,

    //? Newly registered, inside the evaluation period.
    Trial = 1,

    //? Paying / fully enabled workspace.
    Active = 2,

    //? Access disabled (non-payment, abuse, offboarding) — data retained.
    Suspended = 3,
}
