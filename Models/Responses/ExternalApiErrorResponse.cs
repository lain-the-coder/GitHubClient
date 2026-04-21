using System.Text.Json.Serialization;

namespace GitHubClient.Models.Responses;

/// <summary>
/// Represents the error structure returned by the GitHub API.
/// GitHub returns errors as { "message": "...", "documentation_url": "..." }
/// unlike the senior's external API which uses an errors array.
/// </summary>
public class ExternalApiErrorResponse
{
    /// <summary>Error message from GitHub.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Link to relevant GitHub documentation.</summary>
    [JsonPropertyName("documentation_url")]
    public string? DocumentationUrl { get; set; }
}