using System.Text.Json.Serialization;

namespace GitHubClient.Models.Responses;

/// <summary>
/// Response model representing a newly created GitHub repository.
/// Used for Swagger documentation — not deserialized in code (raw JSON passthrough).
/// Same shape as <see cref="GitHubRepositoryResponse"/> — GitHub returns the full repo object on creation.
/// </summary>
public class GitHubCreateRepoResponse
{
    /// <summary>Repository ID.</summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>Repository name.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Full repository name (owner/repo).</summary>
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    /// <summary>Whether the repository is private.</summary>
    [JsonPropertyName("private")]
    public bool IsPrivate { get; set; }

    /// <summary>Repository owner information.</summary>
    [JsonPropertyName("owner")]
    public GitHubRepositoryOwner? Owner { get; set; }

    /// <summary>Repository HTML URL.</summary>
    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; set; }

    /// <summary>Repository description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Whether the repository is a fork.</summary>
    [JsonPropertyName("fork")]
    public bool Fork { get; set; }

    /// <summary>API URL for the repository.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>Primary programming language.</summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>Number of stargazers.</summary>
    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; set; }

    /// <summary>Number of watchers.</summary>
    [JsonPropertyName("watchers_count")]
    public int WatchersCount { get; set; }

    /// <summary>Number of forks.</summary>
    [JsonPropertyName("forks_count")]
    public int ForksCount { get; set; }

    /// <summary>Number of open issues.</summary>
    [JsonPropertyName("open_issues_count")]
    public int OpenIssuesCount { get; set; }

    /// <summary>Default branch name.</summary>
    [JsonPropertyName("default_branch")]
    public string? DefaultBranch { get; set; }

    /// <summary>Repository creation timestamp.</summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>Last update timestamp.</summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>Last push timestamp.</summary>
    [JsonPropertyName("pushed_at")]
    public string? PushedAt { get; set; }
}