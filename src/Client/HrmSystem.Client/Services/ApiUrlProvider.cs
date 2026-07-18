namespace HrmSystem.Client.Services;

/*
    //?     Resolves API-relative paths against the configured API origin.
    //>       Development: appsettings.Development.json → "ApiBaseUrl": "http://localhost:5199"
    //>       Production:  "" → same origin; nginx proxies /api and /hubs to the API container.
*/
public sealed class ApiUrlProvider(string apiBaseUrl, string clientBaseAddress)
{
    public Uri BaseAddress { get; } =
        new(string.IsNullOrWhiteSpace(apiBaseUrl) ? clientBaseAddress : apiBaseUrl);

    public string Resolve(string relativePath) =>
        new Uri(BaseAddress, relativePath).ToString();
}
