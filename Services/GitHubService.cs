using System.Net.Http.Headers;
using GitHubClient.Configuration;
using GitHubClient.Models.Requests;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace GitHubClient.Services;

/// <summary>
/// Implements GitHub API operations by calling the external API with proper authorization.
/// </summary>
public class GitHubService : IGitHubService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly ExternalApisOptions _options;
    private readonly ILogger<GitHubService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GitHubService"/>.
    /// </summary>
    public GitHubService(
        IHttpClientFactory httpClientFactory,
        IAuthorizationService authorizationService,
        IOptions<ExternalApisOptions> options,
        ILogger<GitHubService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _authorizationService = authorizationService;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetAuthenticatedUserAsync(GitHubQueryParameters queryParameters, string transactionId, CancellationToken cancellationToken = default)
    {
        var token = await _authorizationService.GetAccessTokenAsync(transactionId, cancellationToken);
        var client = CreateAuthorizedClient(token);

        var url = $"{_options.GitHubApi.BaseUrl}/user";

        _logger.LogInformation(
            "********************************************\n" +
            "GitHubService :: GetAuthenticatedUserAsync :: Calling external GET\n" +
            "URL: {Url}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            url, transactionId);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                "********************************************\n" +
                "GitHubService :: GetAuthenticatedUserAsync :: FAILED :: Request timed out\n" +
                "URL: {Url}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                url, transactionId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                "********************************************\n" +
                "GitHubService :: GetAuthenticatedUserAsync :: FAILED :: Network error\n" +
                "URL: {Url}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                url, transactionId);
            throw;
        }
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var statusLabel = response.IsSuccessStatusCode ? "SUCCESS" : "FAILED";
        _logger.LogInformation(
            "********************************************\n" +
            "GitHubService :: GetAuthenticatedUserAsync :: httpstatuscode :: {StatusCode}\n" +
            "********************************************\n" +
            "GitHubService :: GetAuthenticatedUserAsync :: {StatusLabel}\n" +
            "GitHubService :: GetAuthenticatedUserAsync :: responseBody :: {ResponseBody}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            response.StatusCode, statusLabel, responseBody, transactionId);

        // Reset content so controller can read it
        response.Content = new StringContent(responseBody, Encoding.UTF8, "application/json");

        return response;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetUserByUsernameAsync(GitHubUsernameParameters usernameParameters, string transactionId, CancellationToken cancellationToken = default)
    {
        var token = await _authorizationService.GetAccessTokenAsync(transactionId, cancellationToken);
        var client = CreateAuthorizedClient(token);

        var url = $"{_options.GitHubApi.BaseUrl}/users/{usernameParameters.Username}";

        _logger.LogInformation(
            "********************************************\n" +
            "GitHubService :: GetUserByUsernameAsync :: Calling external GET\n" +
            "URL: {Url}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            url, transactionId);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                "********************************************\n" +
                "GitHubService :: GetUserByUsernameAsync :: FAILED :: Request timed out\n" +
                "URL: {Url}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                url, transactionId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                "********************************************\n" +
                "GitHubService :: GetUserByUsernameAsync :: FAILED :: Network error\n" +
                "URL: {Url}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                url, transactionId);
            throw;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var statusLabel = response.IsSuccessStatusCode ? "SUCCESS" : "FAILED";
        _logger.LogInformation(
            "********************************************\n" +
            "GitHubService :: GetUserByUsernameAsync :: httpstatuscode :: {StatusCode}\n" +
            "********************************************\n" +
            "GitHubService :: GetUserByUsernameAsync :: {StatusLabel}\n" +
            "GitHubService :: GetUserByUsernameAsync :: responseBody :: {ResponseBody}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            response.StatusCode, statusLabel, responseBody, transactionId);

        // Reset content so controller can read it
        response.Content = new StringContent(responseBody, Encoding.UTF8, "application/json");

        return response;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetUserReposAsync(GitHubUsernameParameters usernameParameters, GitHubUserReposQueryParameters queryParameters, string transactionId, CancellationToken cancellationToken = default)
    {
        var token = await _authorizationService.GetAccessTokenAsync(transactionId, cancellationToken);
        var client = CreateAuthorizedClient(token);

        var url = $"{_options.GitHubApi.BaseUrl}/users/{usernameParameters.Username}/repos{BuildQueryString(queryParameters)}";

        _logger.LogInformation(
            "********************************************\n" +
            "GitHubService :: GetUserReposAsync :: Calling external GET\n" +
            "URL: {Url}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            url, transactionId);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                "********************************************\n" +
                "GitHubService :: GetUserReposAsync :: FAILED :: Request timed out\n" +
                "URL: {Url}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                url, transactionId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                "********************************************\n" +
                "GitHubService :: GetUserReposAsync :: FAILED :: Network error\n" +
                "URL: {Url}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                url, transactionId);
            throw;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var statusLabel = response.IsSuccessStatusCode ? "SUCCESS" : "FAILED";
        _logger.LogInformation(
            "********************************************\n" +
            "GitHubService :: GetUserReposAsync :: httpstatuscode :: {StatusCode}\n" +
            "********************************************\n" +
            "GitHubService :: GetUserReposAsync :: {StatusLabel}\n" +
            "GitHubService :: GetUserReposAsync :: responseBody :: {ResponseBody}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            response.StatusCode, statusLabel, responseBody, transactionId);

        // Reset content so controller can read it
        response.Content = new StringContent(responseBody, Encoding.UTF8, "application/json");

        return response;
    }

    /// <summary>
    /// Builds a query string from non-null query parameters.
    /// Returns empty string when no parameters are provided.
    /// </summary>
    private static string BuildQueryString(GitHubUserReposQueryParameters queryParameters)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrEmpty(queryParameters.Sort))
        {
            queryParts.Add($"sort={Uri.EscapeDataString(queryParameters.Sort)}");
        }

        if (queryParameters.PerPage.HasValue)
        {
            queryParts.Add($"per_page={queryParameters.PerPage.Value}");
        }

        if (queryParameters.Page.HasValue)
        {
            queryParts.Add($"page={queryParameters.Page.Value}");
        }

        return queryParts.Count > 0 ? $"?{string.Join("&", queryParts)}" : string.Empty;
    }

    /// <summary>
    /// Creates an HttpClient with the Bearer authorization header, User-Agent, and Accept headers set.
    /// GitHub requires User-Agent and Accept headers on every request.
    /// </summary>
    private HttpClient CreateAuthorizedClient(string token)
    {
        var client = _httpClientFactory.CreateClient("GitHubClient");
        client.Timeout = TimeSpan.FromSeconds(_options.GitHubApi.TimeoutSeconds);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyAppName", "1.0"));
        return client;
    }
}