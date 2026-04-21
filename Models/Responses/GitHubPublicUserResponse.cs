using System.Text.Json.Serialization;

namespace GitHubClient.Models.Responses;

/// <summary>
/// Response model representing a public GitHub user profile.
/// Used for Swagger documentation — not deserialized in code (raw JSON passthrough).
/// </summary>
public class GitHubPublicUserResponse
{
    /// <summary>GitHub username.</summary>
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    /// <summary>GitHub user ID.</summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>Avatar image URL.</summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>Profile URL.</summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    /// <summary>User type (e.g. User, Organization).</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Display name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Company name.</summary>
    [JsonPropertyName("company")]
    public string? Company { get; set; }

    /// <summary>Blog or website URL.</summary>
    [JsonPropertyName("blog")]
    public string? Blog { get; set; }

    /// <summary>Location.</summary>
    [JsonPropertyName("location")]
    public string? Location { get; set; }

    /// <summary>Email address (may be null for public profiles).</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>User bio.</summary>
    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    /// <summary>Twitter/X username.</summary>
    [JsonPropertyName("twitter_username")]
    public string? TwitterUsername { get; set; }

    /// <summary>Number of public repositories.</summary>
    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; }

    /// <summary>Number of public gists.</summary>
    [JsonPropertyName("public_gists")]
    public int PublicGists { get; set; }

    /// <summary>Number of followers.</summary>
    [JsonPropertyName("followers")]
    public int Followers { get; set; }

    /// <summary>Number of users being followed.</summary>
    [JsonPropertyName("following")]
    public int Following { get; set; }

    /// <summary>Account creation timestamp.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}