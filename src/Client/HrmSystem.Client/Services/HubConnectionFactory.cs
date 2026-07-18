using Microsoft.AspNetCore.SignalR.Client;

namespace HrmSystem.Client.Services;

/*
    //?     One place to build hub connections with the right base URL and JWT wiring:
    //>       await using HubConnection hub = factory.Create("/hubs/analytics");
    //?     AccessTokenProvider delegates to AuthService, so a connection that outlives the
    //?     15-minute access token transparently reconnects with a refreshed one (plan risk 7).
    //!     /hubs/status is anonymous — Create() still attaches the provider; the server
    //!     simply ignores credentials on anonymous hubs.
*/
public sealed class HubConnectionFactory(AuthService authService, ApiUrlProvider apiUrls)
{
    public HubConnection Create(string hubPath) =>
        new HubConnectionBuilder()
            .WithUrl(
                apiUrls.Resolve(hubPath),
                options =>
                {
                    options.AccessTokenProvider = async () =>
                        await authService.GetValidAccessTokenAsync();
                }
            )
            .WithAutomaticReconnect()
            .Build();
}
