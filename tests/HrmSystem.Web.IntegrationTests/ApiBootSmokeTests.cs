using System.Net;
using Shouldly;
using Xunit;

namespace HrmSystem.Web.IntegrationTests;

/*
    //?     End-to-end boot smoke tests: if the composition root miswires ANYTHING
    //?     (DI registration, migrations, auth policies, health checks), these fail —
    //?     they exercise the real HTTP pipeline, not mocks.
*/
public sealed class ApiBootSmokeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Liveness_Returns200_AfterFullBoot()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublicStatus_IsReachable_Anonymously()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("overallStatus");
    }

    [Fact]
    public async Task ProtectedResource_Returns401_WithoutToken()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/employees");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnknownLogin_Returns401_NotServerError()
    {
        using HttpClient client = factory.CreateClient();

        using var payload = new StringContent(
            """{"identifier":"nobody","password":"wrong-password"}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );
        using HttpResponseMessage response = await client.PostAsync("/api/auth/login", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
