using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Entities.Tenants;
using HrmSystem.Domain.Entities.Tenants.ValueObjects;
using Shouldly;
using Xunit;

namespace HrmSystem.Application.UnitTests.Domain.Tenants;

public class TenantIdTests
{
    [Fact]
    public void New_Should_GeneratePrefixedUuid()
    {
        var id = TenantId.New();

        id.Value.ShouldStartWith(TenantId.Prefix);
        Guid.TryParse(id.Value[TenantId.Prefix.Length..], out _).ShouldBeTrue();
    }

    [Fact]
    public void From_Should_ReturnValidationError_WhenValueIsNullOrWhitespace()
    {
        Result<TenantId> nullResult = TenantId.From(null);
        Result<TenantId> blankResult = TenantId.From("   ");

        nullResult.IsFailure.ShouldBeTrue();
        nullResult.FirstError.ShouldBe(TenantErrors.Id.Invalid);
        blankResult.FirstError.ShouldBe(TenantErrors.Id.Invalid);
    }

    [Fact]
    public void From_Should_ReturnFormatError_WhenPrefixIsMissing()
    {
        Result<TenantId> result = TenantId.From(Guid.CreateVersion7().ToString());

        result.IsFailure.ShouldBeTrue();
        result.FirstError.ShouldBe(TenantErrors.Id.InvalidFormat);
    }

    [Fact]
    public void From_Should_RoundTrip_ValueProducedByNew()
    {
        var original = TenantId.New();

        Result<TenantId> reconstituted = TenantId.From(original.Value);

        reconstituted.IsSuccess.ShouldBeTrue();
        reconstituted.Value.ShouldBe(original);
    }
}
