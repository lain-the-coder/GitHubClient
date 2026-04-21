using System.Text.Json.Serialization;

namespace GitHubClient.Models.Requests;

/// <summary>
/// Request body for creating a repository for the authenticated GitHub user.
/// Maps to GitHub's POST /user/repos request body.
/// </summary>
public class GitHubCreateRepoRequest
{
    /// <summary>Repository name. Required. Must follow GitHub naming rules.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Repository description. Optional.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Whether the repository is private. Defaults to false.</summary>
    [JsonPropertyName("private")]
    public bool Private { get; set; } = false;

    /// <summary>Whether to initialize the repository with a README. Defaults to false.</summary>
    [JsonPropertyName("auto_init")]
    public bool AutoInit { get; set; } = false;
}