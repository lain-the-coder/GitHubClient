using System.Text.Json.Serialization;


namespace GitHubClient.Models.Responses;

/// <summary>
/// OAuth2 token response from the authorization server.
/// </summary>
public class TokenResponse
{
    /// <summary>The access token.</summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Token type (e.g. "Bearer").</summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    /// <summary>Token lifetime in seconds.</summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}