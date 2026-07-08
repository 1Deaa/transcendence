namespace HrmSystem.Infrastructure.Services.Emailing;

/*
    //?     Bound from the [SmtpSettings] section in appsettings.json (or User Secrets in dev).
    //
    //!     Never put real credentials in appsettings.json — use User Secrets locally:
    //!       dotnet user-secrets set "SmtpSettings:Username" "you@gmail.com"
    //!       dotnet user-secrets set "SmtpSettings:Password" "your-app-password"
    //!     In production use environment variables or a secrets manager (Azure Key Vault, etc.).
*/
internal sealed class SmtpSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
}
