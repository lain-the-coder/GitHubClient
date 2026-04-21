namespace GitHubClient.Models.Requests;

/// <summary>
/// Path parameters for the GitHub GET user by username endpoint.
/// </summary>
public class GitHubUsernameParameters
{
    /// <summary>GitHub username to look up.</summary>
    public string Username { get; set; } = string.Empty;
}