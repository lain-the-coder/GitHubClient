using System.Text.Json.Serialization;

namespace GitHubClient.Models.Responses;

/// <summary>
/// Response model representing a GitHub user profile.
/// </summary>
public class GitHubUserResponse
{
    /// <summary>GitHub username.</summary>
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>Email address.</summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>User bio.</summary>
    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    /// <summary>Number of public repositories.</summary>
    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; }

    /// <summary>Number of followers.</summary>
    [JsonPropertyName("followers")]
    public int Followers { get; set; }

    /// <summary>Number of users being followed.</summary>
    [JsonPropertyName("following")]
    public int Following { get; set; }
}