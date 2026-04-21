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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP response message from the GitHub API.</returns>
    Task<HttpResponseMessage> GetAuthenticatedUserAsync(GitHubQueryParameters queryParameters, string transactionId, CancellationToken cancellationToken = default);
}