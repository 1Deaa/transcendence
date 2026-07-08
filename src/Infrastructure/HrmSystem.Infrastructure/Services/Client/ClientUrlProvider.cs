using HrmSystem.Application.Common.Interfaces.Authentication;
using Microsoft.Extensions.Options;

namespace HrmSystem.Infrastructure.Services.Client;

internal sealed class ClientUrlProvider : IClientUrlProvider
{
    private readonly ClientSettings _settings;

    public ClientUrlProvider(IOptions<ClientSettings> options)
    {
        _settings = options.Value;
    }

    public Uri BuildPasswordResetUrl(string email, string token)
    {
        string url =
            $"{_settings.BaseUrl}{_settings.ResetPasswordPath}"
            + $"?email={Uri.EscapeDataString(email)}"
            + $"&token={Uri.EscapeDataString(token)}";

        return new Uri(url);
    }

    public Uri BuildEmailConfirmationUrl(string identityUserId, string token)
    {
        string url =
            $"{_settings.BaseUrl}{_settings.ConfirmEmailPath}"
            + $"?userId={Uri.EscapeDataString(identityUserId)}"
            + $"&token={Uri.EscapeDataString(token)}";

        return new Uri(url);
    }
}
