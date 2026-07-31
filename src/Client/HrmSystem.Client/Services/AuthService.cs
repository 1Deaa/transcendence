using System.Net.Http.Json;
using Blazored.LocalStorage;
using HrmSystem.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace HrmSystem.Client.Services;

/*
    //?     Token lifecycle owner: login, refresh, logout — the ONLY writer of the three
    //?     localStorage keys. Everything else (message handler, hub clients) reads tokens
    //?     through GetValidAccessTokenAsync so the refresh flow lives in exactly one place.
    //!     Uses a dedicated "HrmSystem.Auth" HttpClient (no auth handler) — the handler
    //!     itself calls into this service, so sharing the pipeline would recurse.
*/
public sealed class AuthService(
    IHttpClientFactory httpClientFactory,
    ILocalStorageService localStorage,
    AuthenticationStateProvider authenticationStateProvider
) : IDisposable
{
    public const string AuthHttpClientName = "HrmSystem.Auth";

    //? Refresh slightly before real expiry so in-flight requests never race the deadline.
    private static readonly TimeSpan ExpirySafetyWindow = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task<bool> LoginAsync(string identifier, string password)
    {
        HttpClient client = httpClientFactory.CreateClient(AuthHttpClientName);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/auth/login",
            new { identifier, password }
        );

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        AccessTokensResponse? tokens =
            await response.Content.ReadFromJsonAsync<AccessTokensResponse>();
        if (tokens is null)
        {
            return false;
        }

        await StoreTokensAsync(tokens);
        NotifyStateChanged();
        return true;
    }

    /*
        //?     Landing-page signup: one call creates the company workspace + founder
        //?     account and auto-logs in with the returned tokens.
        //>     Returns null on success, or a human-readable error message on failure.
    */
    public async Task<string?> RegisterCompanyAsync(object payload)
    {
        HttpClient client = httpClientFactory.CreateClient(AuthHttpClientName);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/auth/register-company",
            payload
        );

        if (!response.IsSuccessStatusCode)
        {
            ApiProblem? problem = null;
            try
            {
                problem = await response.Content.ReadFromJsonAsync<ApiProblem>();
            }
            catch (System.Text.Json.JsonException)
            {
                //? Non-JSON error body — generic message below covers it.
            }

            return problem?.Message()
                ?? $"Registration failed ({(int)response.StatusCode}). Please review the form.";
        }

        AccessTokensResponse? tokens =
            await response.Content.ReadFromJsonAsync<AccessTokensResponse>();
        if (tokens is null)
        {
            return "Registration succeeded but sign-in failed — try signing in manually.";
        }

        await StoreTokensAsync(tokens);
        NotifyStateChanged();
        return null;
    }

    public async Task LogoutAsync()
    {
        await localStorage.RemoveItemAsync(JwtAuthenticationStateProvider.AccessTokenKey);
        await localStorage.RemoveItemAsync(JwtAuthenticationStateProvider.RefreshTokenKey);
        await localStorage.RemoveItemAsync(JwtAuthenticationStateProvider.ExpiresOnKey);
        NotifyStateChanged();
    }

    /*
        //?     Returns a token that is valid for at least ExpirySafetyWindow more seconds,
        //?     transparently refreshing when needed; null means "not logged in".
        //!     Serialized behind a SemaphoreSlim — parallel API calls at expiry must not
        //!     each burn the single-use refresh token (rotation invalidates the old one).
    */
    public async Task<string?> GetValidAccessTokenAsync()
    {
        string? accessToken = await localStorage.GetItemAsStringAsync(
            JwtAuthenticationStateProvider.AccessTokenKey
        );
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        DateTime? expiresOnUtc = await ReadExpiryAsync();
        if (expiresOnUtc is null || expiresOnUtc > DateTime.UtcNow + ExpirySafetyWindow)
        {
            return accessToken;
        }

        await _refreshLock.WaitAsync();
        try
        {
            //? Another caller may have refreshed while this one waited on the lock.
            expiresOnUtc = await ReadExpiryAsync();
            if (expiresOnUtc > DateTime.UtcNow + ExpirySafetyWindow)
            {
                return await localStorage.GetItemAsStringAsync(
                    JwtAuthenticationStateProvider.AccessTokenKey
                );
            }

            return await RefreshAsync();
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string?> RefreshAsync()
    {
        string? refreshToken = await localStorage.GetItemAsStringAsync(
            JwtAuthenticationStateProvider.RefreshTokenKey
        );
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        HttpClient client = httpClientFactory.CreateClient(AuthHttpClientName);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "api/auth/refresh",
            new { refreshToken }
        );

        if (!response.IsSuccessStatusCode)
        {
            await LogoutAsync();
            return null;
        }

        AccessTokensResponse? tokens =
            await response.Content.ReadFromJsonAsync<AccessTokensResponse>();
        if (tokens is null)
        {
            await LogoutAsync();
            return null;
        }

        await StoreTokensAsync(tokens);
        NotifyStateChanged();
        return tokens.AccessToken;
    }

    private async Task StoreTokensAsync(AccessTokensResponse tokens)
    {
        await localStorage.SetItemAsStringAsync(
            JwtAuthenticationStateProvider.AccessTokenKey,
            tokens.AccessToken
        );
        await localStorage.SetItemAsStringAsync(
            JwtAuthenticationStateProvider.RefreshTokenKey,
            tokens.RefreshToken
        );
        await localStorage.SetItemAsStringAsync(
            JwtAuthenticationStateProvider.ExpiresOnKey,
            tokens.ExpiresOnUtc.ToString("O")
        );
    }

    private async Task<DateTime?> ReadExpiryAsync()
    {
        string? raw = await localStorage.GetItemAsStringAsync(
            JwtAuthenticationStateProvider.ExpiresOnKey
        );

        return DateTime.TryParse(
            raw,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out DateTime parsed
        )
            ? parsed
            : null;
    }

    private void NotifyStateChanged() =>
        ((JwtAuthenticationStateProvider)authenticationStateProvider).NotifyUserChanged();

    public void Dispose() => _refreshLock.Dispose();
}
