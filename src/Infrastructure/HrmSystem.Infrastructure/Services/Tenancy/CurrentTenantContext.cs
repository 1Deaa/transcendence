using HrmSystem.Application.Common.Authentication;
using HrmSystem.Application.Common.Interfaces.Tenancy;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Microsoft.AspNetCore.Http;

namespace HrmSystem.Infrastructure.Services.Tenancy;

/*
    //?     Default ITenantContext implementation.
    //
    //?     Resolution order (first match wins):
    //?      1. Ambient AsyncLocal scope — opened via BeginScope() by background jobs / seeders.
    //?      2. The "tenant_id" claim on the current HTTP principal (stamped by JwtTokenProvider).
    //
    //!     The AsyncLocal is [static] so a scope opened by a Quartz job flows through every
    //!     await in that job's async call-chain — but never leaks across requests, because
    //!     each request/job execution context gets its own AsyncLocal slot.
    //
    //*     Registered as Scoped for symmetry with IHttpContextAccessor consumers; the class
    //*     itself is stateless apart from the shared AsyncLocal slot.
*/
public sealed class CurrentTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private static readonly AsyncLocal<TenantId?> AmbientTenant = new();

    public TenantId? Current => AmbientTenant.Value ?? ResolveFromClaims();

    public IDisposable BeginScope(TenantId tenantId)
    {
        AmbientTenant.Value = tenantId;
        return new AmbientTenantScope();
    }

    private TenantId? ResolveFromClaims()
    {
        string? claimValue = httpContextAccessor
            .HttpContext?.User?.FindFirst(CustomClaimTypes.TenantId)
            ?.Value;

        if (claimValue is null)
        {
            return null;
        }

        //! A malformed claim resolves to null (host-level) rather than throwing —
        //! the write guard + query filters then deny all tenant data access anyway.
        Result<TenantId> tenantIdResult = TenantId.From(claimValue);
        return tenantIdResult.IsSuccess ? tenantIdResult.Value : null;
    }

    private sealed class AmbientTenantScope : IDisposable
    {
        public void Dispose() => AmbientTenant.Value = null;
    }
}
