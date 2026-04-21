namespace GitHubClient.Configuration;

/// <summary>
/// Strongly-typed configuration for external API settings.
/// </summary>
public class ExternalApisOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ExternalApis";

    /// <summary>GitHub API settings.</summary>
    public GitHubApiOptions GitHubApi { get; set; } = new();

    /// <summary>Authorization API settings.</summary>
    public AuthorizationApiOptions AuthorizationApi { get; set; } = new();
}

/// <summary>
/// GitHub external API configuration.
/// </summary>
public class GitHubApiOptions
{
    /// <summary>Base URL of the GitHub API.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Authorization API configuration with support for multiple auth modes.
/// </summary>
public class AuthorizationApiOptions
{
    /// <summary>Authentication mode: StaticToken, BearerRefresh, or OAuth2.</summary>
    public AuthMode AuthMode { get; set; } = AuthMode.StaticToken;

    /// <summary>Static token value (used when AuthMode is StaticToken).</summary>
    public string StaticToken { get; set; } = string.Empty;

    /// <summary>Token endpoint URL (used when AuthMode is BearerRefresh or OAuth2).</summary>
    public string TokenUrl { get; set; } = string.Empty;

    /// <summary>OAuth2 client ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>OAuth2 client secret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>OAuth2 grant type.</summary>
    public string GrantType { get; set; } = "client_credentials";
}

/// <summary>
/// Supported authentication modes for the authorization service.
/// </summary>
public enum AuthMode
{
    /// <summary>Uses a static token from configuration. No HTTP call needed.</summary>
    StaticToken,

    /// <summary>Fetches a Bearer token via JSON POST. Caches with expiry buffer.</summary>
    BearerRefresh,

    /// <summary>Fetches a token via OAuth2 FormUrlEncoded POST. Caches with expiry buffer.</summary>
    OAuth2
}