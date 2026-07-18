using HrmSystem.Application.Common.Interfaces.Messaging;
using HrmSystem.Domain.Common.Result;
using HrmSystem.Infrastructure;
using HrmSystem.Web.Api.Controllers.ApiBase;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace HrmSystem.ArchitectureTests;

/*
    //?     Clean Architecture layer boundaries, enforced at build time (plan §10.11):
    //>       Domain         → nothing (MediatR.Contracts only)
    //>       Application    → Domain
    //>       Infrastructure → Application (+ Domain)
    //>       Web            → everything
    //!     A failing test here means a reference crept in the WRONG DIRECTION — fix the
    //!     dependency, never the test.
*/
public sealed class LayerDependencyTests
{
    private const string ApplicationNamespace = "HrmSystem.Application";
    private const string InfrastructureNamespace = "HrmSystem.Infrastructure";
    private const string WebNamespace = "HrmSystem.Web";

    private static readonly System.Reflection.Assembly DomainAssembly =
        typeof(Result).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(ICommand).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly =
        typeof(DependencyInjection).Assembly;
    private static readonly System.Reflection.Assembly WebAssembly =
        typeof(ApiBaseController).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOn_AnyOuterLayer()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, WebNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReport(result));
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureOrWeb()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, WebNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReport(result));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Web()
    {
        TestResult result = Types
            .InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(WebNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReport(result));
    }

    [Fact]
    public void CommandAndQueryHandlers_ShouldLiveIn_Application()
    {
        //! IQueryHandler is internal to Application — selection is by naming convention.
        TestResult result = Types
            .InAssemblies([ApplicationAssembly, InfrastructureAssembly, WebAssembly])
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Or()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .ResideInNamespaceStartingWith(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailureReport(result));
    }

    private static string FailureReport(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "Offending types: "
                + string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
