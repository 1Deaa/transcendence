using HrmSystem.Domain.Common.Result;
using HrmSystem.Domain.Common.Result.Errors;
using Shouldly;
using Xunit;

namespace HrmSystem.Application.UnitTests.Domain.Common;

public class ResultTests
{
    private static readonly Error TestError = Error.Validation("Test.Invalid", "Test error.");

    [Fact]
    public void Success_Should_HaveNoErrors_And_FirstErrorIsNone()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
        result.FirstError.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_Should_ExposeError_And_BeFailure()
    {
        var result = Result.Failure(TestError);

        result.IsFailure.ShouldBeTrue();
        result.FirstError.ShouldBe(TestError);
    }

    [Fact]
    public void ImplicitConversion_FromError_Should_ProduceFailure()
    {
        Result result = TestError;

        result.IsFailure.ShouldBeTrue();
        result.FirstError.ShouldBe(TestError);
    }

    [Fact]
    public void ImplicitConversion_FromValue_Should_ProduceSuccessWithValue()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Match_Should_InvokeCorrectBranch()
    {
        Result<int> success = 7;
        var failure = Result.Failure<int>(TestError);

        string successOutcome = success.Match(value => $"ok:{value}", _ => "fail");
        string failureOutcome = failure.Match(value => $"ok:{value}", errors => errors[0].Code);

        successOutcome.ShouldBe("ok:7");
        failureOutcome.ShouldBe("Test.Invalid");
    }

    [Fact]
    public void UnavailableFactory_Should_CarryUnavailableErrorType()
    {
        var error = Error.Unavailable("Backups.SchedulerUnavailable", "The scheduler is down.");

        error.Type.ShouldBe(ErrorType.Unavailable);
        error.Code.ShouldBe("Backups.SchedulerUnavailable");
    }
}
