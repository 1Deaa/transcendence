using System.Reflection;
using HrmSystem.Application.Common.Interfaces.Data.Queries;
using HrmSystem.Domain.Common.Abstractions;
using HrmSystem.Domain.Common.Interfaces;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Shouldly;
using Xunit;

namespace HrmSystem.ArchitectureTests;

/*
    //!     Dapper BYPASSES the EF named query filters — there is NO safety net on the read
    //!     side except the convention that every tenant-scoped Dapper method takes an
    //!     explicit TenantId (CLAUDE.md ABSOLUTE rule). These tests make the convention
    //!     mechanical: adding a tenant-scoped query method without a TenantId parameter
    //!     fails the build's test run, not a code review three weeks later.
    //
    //?     IStatusQueries is exempt BY DESIGN: it reads host-level operations tables
    //?     (HealthCheckSnapshots, BackupHistory) that are never tenant-owned.
*/
public sealed class TenantSafetyTests
{
    [Fact]
    public void EveryAnalyticsQueryMethod_MustTakeAn_ExplicitTenantId()
    {
        MethodInfo[] methods = typeof(IAnalyticsQueries).GetMethods();

        methods.ShouldNotBeEmpty();

        var offenders = methods
            .Where(method =>
                !method.GetParameters().Any(p => p.ParameterType == typeof(TenantId))
            )
            .Select(method => method.Name)
            .ToList();

        offenders.ShouldBeEmpty(
            $"Dapper bypasses EF tenant filters — these IAnalyticsQueries methods have no TenantId parameter: {string.Join(", ", offenders)}"
        );
    }

    [Fact]
    public void EveryChatQueryMethod_MustTakeAn_ExplicitTenantId()
    {
        MethodInfo[] methods = typeof(IChatQueries).GetMethods();

        methods.ShouldNotBeEmpty();

        var offenders = methods
            .Where(method =>
                !method.GetParameters().Any(p => p.ParameterType == typeof(TenantId))
            )
            .Select(method => method.Name)
            .ToList();

        offenders.ShouldBeEmpty(
            $"Dapper bypasses EF tenant filters — these IChatQueries methods have no TenantId parameter: {string.Join(", ", offenders)}"
        );
    }

    [Fact]
    public void TenantScopedAdminQueryMethods_MustTakeAn_ExplicitTenantId()
    {
        //? GetRolesWithPermissionsAsync reads host-level Identity metadata — exempt by design.
        var offenders = typeof(IAdminQueries)
            .GetMethods()
            .Where(method => method.Name != nameof(IAdminQueries.GetRolesWithPermissionsAsync))
            .Where(method =>
                !method.GetParameters().Any(p => p.ParameterType == typeof(TenantId))
            )
            .Select(method => method.Name)
            .ToList();

        offenders.ShouldBeEmpty(
            $"Dapper bypasses EF tenant filters — these IAdminQueries methods have no TenantId parameter: {string.Join(", ", offenders)}"
        );
    }

    [Fact]
    public void TenantOwnedAggregates_MustDeriveFrom_ATenantEntity()
    {
        /*
            //?     ITenantOwned is what the write guard + query filters key on; ATenantEntity
            //?     is the only sanctioned way to become ITenantOwned. Implementing the
            //?     interface directly would skip the shared TenantId plumbing.
        */
        var offenders = typeof(Result).Assembly
            .GetTypes()
            .Where(type =>
                typeof(ITenantOwned).IsAssignableFrom(type)
                && type is { IsClass: true, IsAbstract: false }
                && !IsDerivedFromGeneric(type, typeof(ATenantEntity<>))
            )
            .Select(type => type.FullName)
            .ToList();

        offenders.ShouldBeEmpty(
            $"Tenant-owned types must derive from ATenantEntity<TId>: {string.Join(", ", offenders)}"
        );
    }

    private static bool IsDerivedFromGeneric(Type type, Type openGeneric)
    {
        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
            {
                return true;
            }
        }

        return false;
    }
}
