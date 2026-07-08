namespace HrmSystem.Infrastructure.Services.Client;

/*
    //?     Bound from the [ClientSettings] section in appsettings.json.
    //?     Carries the base URL and path segments used to build deep-links inside transactional emails.
*/
internal sealed class ClientSettings
{
    public string BaseUrl { get; init; } = string.Empty;
    public string ResetPasswordPath { get; init; } = string.Empty;
    public string ConfirmEmailPath { get; init; } = string.Empty;
}
