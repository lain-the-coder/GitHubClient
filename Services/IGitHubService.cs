using GitHubClient.Models.Requests;

namespace GitHubClient.Services;

/// <summary>
/// Handles GitHub API operations against the external API.
/// </summary>
public interface IGitHubService
{
    /// <summary>
    /// Retrieves the authenticated user's profile from the GitHub API.
    /// </summary>
    /// <param name="queryParameters">Query parameters for the request.</param>
    /// <param name="transactionId">Unique transaction identifier for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP response message from the GitHub API.</returns>
    Task<HttpResponseMessage> GetAuthenticatedUserAsync(GitHubQueryParameters queryParameters, string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a public user profile by username from the GitHub API.
    /// </summary>
    /// <param name="usernameParameters">Path parameters containing the username.</param>
    /// <param name="transactionId">Unique transaction identifier for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP response message from the GitHub API.</returns>
    Task<HttpResponseMessage> GetUserByUsernameAsync(GitHubUsernameParameters usernameParameters, string transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves repositories for a public user from the GitHub API.
    /// </summary>
    /// <param name="usernameParameters">Path parameters containing the username.</param>
    /// <param name="queryParameters">Query parameters for sorting and pagination.</param>
    /// <param name="transactionId">Unique transaction identifier for logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP response message from the GitHub API.</returns>
    Task<HttpResponseMessage> GetUserReposAsync(GitHubUsernameParameters usernameParameters, GitHubUserReposQueryParameters queryParameters, string transactionId, CancellationToken cancellationToken = default);
}