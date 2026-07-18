using System.Net;
using System.Net.Http.Headers;

namespace HrmSystem.Client.Services;

/*
    //?     DelegatingHandler on the main API HttpClient: attaches the bearer token to every
    //?     request, refreshing it first when it is about to expire (AuthService owns that).
    //!     Registered ONLY on the "HrmSystem.Api" client — the "HrmSystem.Auth" client used
    //!     by AuthService itself stays handler-free to avoid login/refresh recursion.
*/
public sealed class AuthorizationMessageHandler(AuthService authService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        string? accessToken = await authService.GetValidAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken
            );
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        //? 401 despite a locally-valid token → revoked/blocked server-side. Drop the session.
        if (response.StatusCode == HttpStatusCode.Unauthorized && accessToken is not null)
        {
            await authService.LogoutAsync();
        }

        return response;
    }
}
