using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using HrmSystem.Domain.Entities.Tenants.Enums;
using HrmSystem.Domain.Entities.Tenants.Events;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;

namespace HrmSystem.Domain.Entities.Tenants;

/*
    //?     The Tenant aggregate root — one row per registered company workspace.
    //!     HOST-LEVEL: does NOT derive from ATenantEntity and is never filtered by tenant.
    //?     Rich Domain Model: private ctor + static factory, private setters, named transitions.
*/
public class Tenant : AFullAuditableEntity<TenantId>
{
    #region Constructor

    private Tenant(TenantId id, CompanyName companyName, TenantSlug slug, TenantStatus status)
        : base(id)
    {
        CompanyName = companyName;
        Slug = slug;
        Status = status;
    }

    //! Private Parameterless Constructor: for EF Core materialisation only — never use in domain or application code.
    private Tenant()
        : base() { }

    #endregion

    #region Properties

    //? Trimmed, non-empty company display name. Max CompanyName.MaxLength characters.
    public CompanyName CompanyName { get; private set; } = null!;

    //? URL-safe workspace identifier ("acme"). Immutable after creation.
    public TenantSlug Slug { get; private set; } = null!;

    public TenantStatus Status { get; private set; }

    //? UTC timestamp of the most recent suspension; null while not suspended.
    public DateTime? SuspendedAt { get; private set; }

    #endregion

    #region Static factory

    //? The only way to create a valid Tenant — collects every broken rule before returning.
    //> New workspaces start in TenantStatus.Trial; billing flows promote them to Active later.
    public static Result<Tenant> Create(string companyName, string slug)
    {
        var errors = new List<Error>();

        Result<CompanyName> companyNameResult = CompanyName.Create(companyName);
        if (companyNameResult.IsFailure)
        {
            errors.AddRange(companyNameResult.Errors);
        }

        Result<TenantSlug> slugResult = TenantSlug.Create(slug);
        if (slugResult.IsFailure)
        {
            errors.AddRange(slugResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var tenant = new Tenant(
            TenantId.New(),
            companyNameResult.Value,
            slugResult.Value,
            TenantStatus.Trial
        );
        tenant.RaiseDomainEvent(new TenantCreatedDomainEvent(tenant.Id!));

        return tenant;
    }

    #endregion

    #region Domain methods

    //? Promotes the workspace to a fully enabled (paying) tenant.
    public Result ActivateSubscription()
    {
        if (Status == TenantStatus.Active)
        {
            return TenantErrors.AlreadyActive;
        }

        Status = TenantStatus.Active;
        SuspendedAt = null;

        return Result.Success();
    }

    //? Disables access while retaining all data (non-payment, abuse, offboarding).
    public Result Suspend(DateTime suspendedAtUtc)
    {
        if (Status == TenantStatus.Suspended)
        {
            return TenantErrors.AlreadySuspended;
        }

        Status = TenantStatus.Suspended;
        SuspendedAt = suspendedAtUtc;

        return Result.Success();
    }

    //? Updates the company display name. The slug is immutable — it lives in URLs and emails.
    public Result UpdateCompanyName(string companyName)
    {
        Result<CompanyName> companyNameResult = CompanyName.Create(companyName);
        if (companyNameResult.IsFailure)
        {
            return companyNameResult.Errors.ToList();
        }

        CompanyName = companyNameResult.Value;

        return Result.Success();
    }

    #endregion
}
