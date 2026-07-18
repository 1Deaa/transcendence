using Blazored.LocalStorage;
using HrmSystem.Client;
using HrmSystem.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

/*
    //?     ApiBaseUrl comes from wwwroot/appsettings{.Environment}.json:
    //>       Development → http://localhost:5199 (API run directly, CORS-enabled)
    //>       Production  → "" (same origin; nginx proxies /api and /hubs)
*/
string apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? string.Empty;
var apiUrls = new ApiUrlProvider(apiBaseUrl, builder.HostEnvironment.BaseAddress);
builder.Services.AddSingleton(apiUrls);

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<JwtAuthenticationStateProvider>()
);
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<HubConnectionFactory>();

/*
    //!     Two named clients on purpose (see AuthService):
    //!       "HrmSystem.Api"  — bearer-token handler attached; used by all pages.
    //!       "HrmSystem.Auth" — bare pipeline for login/refresh; prevents handler recursion.
*/
builder.Services
    .AddHttpClient(
        "HrmSystem.Api",
        client => client.BaseAddress = apiUrls.BaseAddress
    )
    .AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddHttpClient(
    AuthService.AuthHttpClientName,
    client => client.BaseAddress = apiUrls.BaseAddress
);

//? Default HttpClient injected into pages = the authenticated API client.
builder.Services.AddScoped(serviceProvider =>
    serviceProvider
        .GetRequiredService<IHttpClientFactory>()
        .CreateClient("HrmSystem.Api")
);

await builder.Build().RunAsync();
