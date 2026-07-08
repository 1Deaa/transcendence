namespace HrmSystem.Infrastructure.Services.Jwt;

public sealed class JwtAuthOptions
{
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public string Key { get; init; }
    public int TokenExpirationInMinutes { get; init; }
    public int RefreshTokenExpirationDays { get; init; }
}
