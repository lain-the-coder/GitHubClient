using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GitHubClient.Configuration;
using GitHubClient.Models.Responses;
using Microsoft.Extensions.Options;

namespace GitHubClient.Services;

/// <summary>
/// Manages token acquisition and caching for external API calls.
/// Thread-safe singleton implementation with automatic token refresh.
/// Supports three authentication modes: StaticToken, BearerRefresh, and OAuth2.
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalApisOptions _options;
    private readonly ILogger<AuthorizationService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>Buffer in seconds before actual expiry to trigger refresh.</summary>
    private const int ExpiryBufferSeconds = 30;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthorizationService"/>.
    /// </summary>
    public AuthorizationService(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalApisOptions> options,
        ILogger<AuthorizationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        return _options.AuthorizationApi.AuthMode switch
        {
            AuthMode.StaticToken => GetStaticToken(transactionId),
            AuthMode.BearerRefresh => await GetCachedTokenAsync(FetchBearerTokenAsync, transactionId, cancellationToken),
            AuthMode.OAuth2 => await GetCachedTokenAsync(FetchOAuth2TokenAsync, transactionId, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported AuthMode: {_options.AuthorizationApi.AuthMode}")
        };
    }

    /// <summary>
    /// Returns the static token directly from configuration. No HTTP call, no caching.
    /// </summary>
    private string GetStaticToken(string transactionId)
    {
        if (string.IsNullOrEmpty(_options.AuthorizationApi.StaticToken))
        {
            throw new InvalidOperationException("StaticToken is not configured.");
        }

        _logger.LogInformation(
            "********************************************\n" +
            "AuthorizationService :: GetStaticToken :: Returning static token from configuration\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            transactionId);

        return _options.AuthorizationApi.StaticToken;
    }

    /// <summary>
    /// Gets a cached token, refreshing via the provided fetch delegate when expired.
    /// Uses SemaphoreSlim with double-checked locking for thread safety.
    /// </summary>
    private async Task<string> GetCachedTokenAsync(
    Func<CancellationToken, Task<TokenResponse>> fetchTokenAsync,
    string transactionId,
    CancellationToken cancellationToken)
    {
        // Fast path: return cached token if still valid
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
        {
            _logger.LogDebug(
                "============================================================\n" +
                "AuthorizationService :: GetCachedTokenAsync :: Returning cached token\n" +
                "TransactionId: {transactionId}\n" +
                "============================================================",
                transactionId);
            return _cachedToken;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            _logger.LogInformation(
                 "********************************************\n" +
                 "AuthorizationService :: GetCachedTokenAsync :: Acquiring new token\n" +
                 "TokenUrl: {TokenUrl}\n" +
                 "TransactionId: {TransactionId}\n" +
                 "********************************************",
                 _options.AuthorizationApi.TokenUrl, transactionId);

            var tokenResponse = await fetchTokenAsync(cancellationToken);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Authorization server returned an empty token.");
            }

            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - ExpiryBufferSeconds);

            _logger.LogInformation(
                    "********************************************\n" +
                    "AuthorizationService :: GetCachedTokenAsync :: Token acquired :: SUCCESS\n" +
                    "Expires at: {Expiry} UTC\n" +
                    "TransactionId: {TransactionId}\n" +
                    "********************************************",
                    _tokenExpiry, transactionId);

            return _cachedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "============================================================\n" +
                "AuthorizationService :: GetCachedTokenAsync :: Failed to acquire token\n" +
                "TransactionId: {transactionId}\n" +
                "============================================================",
                transactionId);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Fetches a Bearer token via JSON POST with client_id and client_secret.
    /// </summary>
    private async Task<TokenResponse> FetchBearerTokenAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("AuthClient");

        var requestBody = new StringContent(
            JsonSerializer.Serialize(new
            {
                client_id = _options.AuthorizationApi.ClientId,
                client_secret = _options.AuthorizationApi.ClientSecret
            }),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(_options.AuthorizationApi.TokenUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        return tokenResponse!;
    }

    /// <summary>
    /// Fetches a token via OAuth2 FormUrlEncoded POST with grant_type, client_id, and client_secret.
    /// Matches the senior's AuthorizationService implementation exactly.
    /// </summary>
    private async Task<TokenResponse> FetchOAuth2TokenAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("AuthClient");

        var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = _options.AuthorizationApi.GrantType,
            ["client_id"] = _options.AuthorizationApi.ClientId,
            ["client_secret"] = _options.AuthorizationApi.ClientSecret
        });

        var response = await client.PostAsync(_options.AuthorizationApi.TokenUrl, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        return tokenResponse!;
    }
}