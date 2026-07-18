using HrmSystem.Application.Common.Exceptions;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Departments;
using HrmSystem.Domain.Entities.Departments.ValueObjects;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using HrmSystem.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HrmSystem.Infrastructure.IntegrationTests.Tenancy;

/*
    //?     Proves the two halves of tenant isolation against REAL SQL Server:
    //?      - read side:  the named "Tenant" EF query filter (ApplicationDbContext)
    //?      - write side: TenantWriteGuardInterceptor stamping + cross-tenant rejection
    //!     These are the guarantees the whole multi-tenancy design leans on (plan §6) —
    //!     if one of these tests fails, tenant data is leaking.
*/
public sealed class TenantIsolationTests(SqlServerFixture fixture)
    : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Writes_AreStampedWithAmbientTenant_AndReadsAreIsolated()
    {
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();

        //? Tenant A hires a department.
        DepartmentId departmentId;
        using (fixture.TenantContext.BeginScope(tenantA))
        {
            await using ApplicationDbContext context = fixture.CreateContext();
            Result<Department> created = Department.Create("Engineering", "ENG");
            created.IsSuccess.ShouldBeTrue();

            context.Departments.Add(created.Value);
            await context.SaveChangesAsync();

            //! The interceptor — not the domain factory — must have stamped the TenantId.
            created.Value.TenantId.ShouldBe(tenantA);
            departmentId = created.Value.Id!;
        }

        //? Tenant A sees it.
        using (fixture.TenantContext.BeginScope(tenantA))
        {
            await using ApplicationDbContext context = fixture.CreateContext();
            bool visibleToOwner = await context.Departments.AnyAsync(d => d.Id == departmentId);
            visibleToOwner.ShouldBeTrue();
        }

        //? Tenant B does not.
        using (fixture.TenantContext.BeginScope(tenantB))
        {
            await using ApplicationDbContext context = fixture.CreateContext();
            bool visibleToStranger = await context.Departments.AnyAsync(d =>
                d.Id == departmentId
            );
            visibleToStranger.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task HostContext_WithoutTenant_SeesNoTenantRows()
    {
        var tenantA = TenantId.New();

        using (fixture.TenantContext.BeginScope(tenantA))
        {
            await using ApplicationDbContext context = fixture.CreateContext();
            Result<Department> created = Department.Create("Finance", "FIN");
            context.Departments.Add(created.Value);
            await context.SaveChangesAsync();
        }

        //! Current == null → the "Tenant" filter compares against null → zero rows.
        fixture.TenantContext.Current = null;
        await using ApplicationDbContext hostContext = fixture.CreateContext();
        int hostVisibleRows = await hostContext.Departments.CountAsync();
        hostVisibleRows.ShouldBe(0);

        //? Host admin can opt in explicitly — soft-delete filter stays active.
        int optedInRows = await hostContext
            .Departments.IgnoreQueryFilters(["Tenant"])
            .CountAsync();
        optedInRows.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CrossTenantModification_IsRejected_BeforeReachingTheDatabase()
    {
        var tenantA = TenantId.New();
        var tenantB = TenantId.New();

        DepartmentId departmentId;
        using (fixture.TenantContext.BeginScope(tenantA))
        {
            await using ApplicationDbContext context = fixture.CreateContext();
            Result<Department> created = Department.Create("Sales", "SLS");
            context.Departments.Add(created.Value);
            await context.SaveChangesAsync();
            departmentId = created.Value.Id!;
        }

        /*
            //?     Simulates a compromised/buggy handler: load a row while impersonating
            //?     its owner, then try to SAVE it under a different ambient tenant.
        */
        await using ApplicationDbContext attackContext = fixture.CreateContext();

        Department victim;
        using (fixture.TenantContext.BeginScope(tenantA))
        {
            victim = await attackContext.Departments.SingleAsync(d => d.Id == departmentId);
        }

        using (fixture.TenantContext.BeginScope(tenantB))
        {
            Result renameResult = victim.Update("Stolen Sales", "SLS");
            renameResult.IsSuccess.ShouldBeTrue();

            await Should.ThrowAsync<CrossTenantWriteException>(async () =>
                await attackContext.SaveChangesAsync()
            );
        }
    }
}
