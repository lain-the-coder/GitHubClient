namespace GitHubClient.Services;

/// <summary>
/// Provides access tokens for authenticating with external APIs.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Gets a valid access token, refreshing if expired.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A valid Bearer access token.</returns>
    Task<string> GetAccessTokenAsync(string transactionId, CancellationToken cancellationToken = default);
}