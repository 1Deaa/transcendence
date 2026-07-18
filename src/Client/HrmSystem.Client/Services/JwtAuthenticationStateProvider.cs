using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace HrmSystem.Client.Services;

/*
    //?     AuthenticationState from the JWT held in localStorage — no server round-trip:
    //?     the token payload already carries sub/name/role/tenant_id/permission claims,
    //?     so the client parses them locally to drive [AuthorizeView] / [AuthorizeRouteView].
    //!     Client-side claims are for UX only (menu visibility, redirects). The API re-checks
    //!     every permission server-side on every call — nothing here is a security boundary.
*/
public sealed class JwtAuthenticationStateProvider(ILocalStorageService localStorage)
    : AuthenticationStateProvider
{
    public const string AccessTokenKey = "hrm.accessToken";
    public const string RefreshTokenKey = "hrm.refreshToken";
    public const string ExpiresOnKey = "hrm.expiresOnUtc";

    private static readonly AuthenticationState Anonymous = new(
        new ClaimsPrincipal(new ClaimsIdentity())
    );

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string? token = await localStorage.GetItemAsStringAsync(AccessTokenKey);
        if (string.IsNullOrWhiteSpace(token))
        {
            return Anonymous;
        }

        List<Claim> claims = ParseClaimsFromJwt(token);
        if (claims.Count == 0)
        {
            return Anonymous;
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyUserChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    /*
        //?     Manual payload decode instead of a JWT library — the client only needs the
        //?     claim set for display purposes; signature validation happens on the server.
    */
    private static List<Claim> ParseClaimsFromJwt(string jwt)
    {
        string[] parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return [];
        }

        try
        {
            byte[] payloadBytes = DecodeBase64Url(parts[1]);
            using var document = JsonDocument.Parse(payloadBytes);

            var claims = new List<Claim>();
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    claims.AddRange(
                        property.Value
                            .EnumerateArray()
                            .Select(item => new Claim(property.Name, item.ToString()))
                    );
                }
                else
                {
                    claims.Add(new Claim(property.Name, property.Value.ToString()));
                }
            }

            //? Map the raw JWT claim names onto what ClaimsIdentity expects for Name/Role.
            //! This token uses [preferred_username]; [role] arrives pre-mapped to the full
            //! schema URI by the server, so no extra role mapping is needed.
            MapWellKnownClaim(claims, "preferred_username", ClaimTypes.Name);
            MapWellKnownClaim(claims, "unique_name", ClaimTypes.Name);
            MapWellKnownClaim(claims, "sub", ClaimTypes.NameIdentifier);
            MapWellKnownClaim(claims, "role", ClaimTypes.Role);

            return claims;
        }
        catch (JsonException)
        {
            return [];
        }
        catch (FormatException)
        {
            return [];
        }
    }

    private static void MapWellKnownClaim(List<Claim> claims, string jwtName, string claimType)
    {
        claims.AddRange(
            claims
                .Where(c => c.Type == jwtName)
                .Select(c => new Claim(claimType, c.Value))
                .ToList()
        );
    }

    private static byte[] DecodeBase64Url(string base64Url)
    {
        string padded = base64Url.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            _ => padded,
        };

        return Convert.FromBase64String(padded);
    }
}
