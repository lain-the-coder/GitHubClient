using GitHubClient.Helpers;
using GitHubClient.Models.Requests;
using GitHubClient.Models.Responses;
using GitHubClient.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace GitHubClient.Controllers;

/// <summary>
/// API controller for GitHub operations.
/// </summary>
[ApiController]
[Route("api/github")]
[Produces("application/json")]
public class GitHubController : ControllerBase
{
    private readonly IGitHubService _gitHubService;
    private readonly IValidator<GitHubQueryParameters> _getValidator;
    private readonly IValidator<GitHubUsernameParameters> _usernameValidator;
    private readonly IValidator<GitHubUserReposQueryParameters> _userReposQueryValidator;
    private readonly ILogger<GitHubController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GitHubController"/>.
    /// </summary>
    public GitHubController(
        IGitHubService gitHubService,
        IValidator<GitHubQueryParameters> getValidator,
        IValidator<GitHubUsernameParameters> usernameValidator,
        IValidator<GitHubUserReposQueryParameters> userReposQueryValidator,
        ILogger<GitHubController> logger)
    {
        _gitHubService = gitHubService;
        _getValidator = getValidator;
        _usernameValidator = usernameValidator;
        _userReposQueryValidator = userReposQueryValidator;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the authenticated GitHub user's profile.
    /// </summary>
    /// <param name="queryParameters">Query parameters (reserved for future use).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The GitHub user profile or a standardized error response.</returns>
    /// <response code="200">User profile retrieved successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Authorization failure.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(GitHubUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAuthenticatedUser(
        [FromQuery] GitHubQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var transactionId = $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
        _logger.LogInformation(
            "********************************************\n" +
            "GitHubController :: GetAuthenticatedUser :: Request received\n" +
            "TransactionId: {transactionId}\n" +
            "********************************************",
            transactionId);

        // Validate query parameters
        var validationResult = await _getValidator.ValidateAsync(queryParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errorResponse = new ErrorResponse
            {
                Errors = validationResult.Errors.Select(e => new ErrorDetail
                {
                    Type = "VALIDATION_ERROR",
                    Code = "1900004",
                    Message = e.ErrorMessage
                }).ToList()
            };

            _logger.LogWarning(
                "********************************************\n" +
                "GitHubController :: GetAuthenticatedUser :: FAILED :: Validation error\n" +
                "Errors: {Errors}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage)),
                transactionId);

            return BadRequest(errorResponse);
        }

        try
        {
            var response = await _gitHubService.GetAuthenticatedUserAsync(queryParameters, transactionId, cancellationToken);

            _logger.LogInformation(
                "********************************************\n" +
                "GitHubController :: GetAuthenticatedUser :: Response received from service :: Processing\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);

            return await HandleExternalResponse(response, transactionId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetAuthenticatedUser :: FAILED :: Request timed out\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(504, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
        {
            new() { Type = "TIMEOUT_ERROR", Code = "9999998", Message = "The request to the external service timed out." }
        }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetAuthenticatedUser :: FAILED :: Configuration error\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(500, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
        {
            new() { Type = "CONFIGURATION_ERROR", Code = "9999997", Message = ex.Message }
        }
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetAuthenticatedUser :: FAILED :: Network error\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(502, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
        {
            new() { Type = "GATEWAY_ERROR", Code = "9999999", Message = "External service unavailable." }
        }
            });
        }
    }

    /// <summary>
    /// Retrieves a public GitHub user profile by username.
    /// </summary>
    /// <param name="username">GitHub username to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public GitHub user profile or a standardized error response.</returns>
    /// <response code="200">Public user profile retrieved successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Authorization failure.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("users/{username}")]
    [ProducesResponseType(typeof(GitHubPublicUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserByUsername(
        [FromRoute] string username,
        CancellationToken cancellationToken)
    {
        var transactionId = $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
        _logger.LogInformation(
            "********************************************\n" +
            "GitHubController :: GetUserByUsername :: Request received\n" +
            "Username: {Username}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            username, transactionId);

        // Validate username path parameter
        var usernameParameters = new GitHubUsernameParameters { Username = username };
        var validationResult = await _usernameValidator.ValidateAsync(usernameParameters, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errorResponse = new ErrorResponse
            {
                Errors = validationResult.Errors.Select(e => new ErrorDetail
                {
                    Type = "VALIDATION_ERROR",
                    Code = "1900004",
                    Message = e.ErrorMessage
                }).ToList()
            };

            _logger.LogWarning(
                "********************************************\n" +
                "GitHubController :: GetUserByUsername :: FAILED :: Validation error\n" +
                "Errors: {Errors}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage)),
                transactionId);

            return BadRequest(errorResponse);
        }

        try
        {
            var response = await _gitHubService.GetUserByUsernameAsync(usernameParameters, transactionId, cancellationToken);

            _logger.LogInformation(
                "********************************************\n" +
                "GitHubController :: GetUserByUsername :: Response received from service :: Processing\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);

            return await HandleExternalResponse(response, transactionId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetUserByUsername :: FAILED :: Request timed out\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(504, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new() { Type = "TIMEOUT_ERROR", Code = "9999998", Message = "The request to the external service timed out." }
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetUserByUsername :: FAILED :: Configuration error\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(500, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new() { Type = "CONFIGURATION_ERROR", Code = "9999997", Message = ex.Message }
                }
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetUserByUsername :: FAILED :: Network error\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(502, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new() { Type = "GATEWAY_ERROR", Code = "9999999", Message = "External service unavailable." }
                }
            });
        }
    }

    /// <summary>
    /// Retrieves repositories for a public GitHub user.
    /// </summary>
    /// <param name="username">GitHub username whose repositories to list.</param>
    /// <param name="queryParameters">Query parameters for sorting and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of repositories or a standardized error response.</returns>
    /// <response code="200">Repositories retrieved successfully.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Authorization failure.</response>
    /// <response code="403">Insufficient permissions.</response>
    /// <response code="404">User not found.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("users/{username}/repos")]
    [ProducesResponseType(typeof(List<GitHubRepositoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUserRepos(
        [FromRoute] string username,
        [FromQuery] GitHubUserReposQueryParameters queryParameters,
        CancellationToken cancellationToken)
    {
        var transactionId = $"{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".Substring(0, 32);
        _logger.LogInformation(
            "********************************************\n" +
            "GitHubController :: GetUserRepos :: Request received\n" +
            "Username: {Username}\n" +
            "TransactionId: {TransactionId}\n" +
            "********************************************",
            username, transactionId);

        // Validate username path parameter
        var usernameParameters = new GitHubUsernameParameters { Username = username };
        var usernameValidationResult = await _usernameValidator.ValidateAsync(usernameParameters, cancellationToken);
        if (!usernameValidationResult.IsValid)
        {
            var errorResponse = new ErrorResponse
            {
                Errors = usernameValidationResult.Errors.Select(e => new ErrorDetail
                {
                    Type = "VALIDATION_ERROR",
                    Code = "1900004",
                    Message = e.ErrorMessage
                }).ToList()
            };

            _logger.LogWarning(
                "********************************************\n" +
                "GitHubController :: GetUserRepos :: FAILED :: Validation error\n" +
                "Errors: {Errors}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                string.Join(" | ", usernameValidationResult.Errors.Select(e => e.ErrorMessage)),
                transactionId);

            return BadRequest(errorResponse);
        }

        // Validate query parameters
        var queryValidationResult = await _userReposQueryValidator.ValidateAsync(queryParameters, cancellationToken);
        if (!queryValidationResult.IsValid)
        {
            var errorResponse = new ErrorResponse
            {
                Errors = queryValidationResult.Errors.Select(e => new ErrorDetail
                {
                    Type = "VALIDATION_ERROR",
                    Code = "1900004",
                    Message = e.ErrorMessage
                }).ToList()
            };

            _logger.LogWarning(
                "********************************************\n" +
                "GitHubController :: GetUserRepos :: FAILED :: Validation error\n" +
                "Errors: {Errors}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                string.Join(" | ", queryValidationResult.Errors.Select(e => e.ErrorMessage)),
                transactionId);

            return BadRequest(errorResponse);
        }

        try
        {
            var response = await _gitHubService.GetUserReposAsync(usernameParameters, queryParameters, transactionId, cancellationToken);

            _logger.LogInformation(
                "********************************************\n" +
                "GitHubController :: GetUserRepos :: Response received from service :: Processing\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);

            return await HandleExternalResponse(response, transactionId);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetUserRepos :: FAILED :: Request timed out\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(504, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new() { Type = "TIMEOUT_ERROR", Code = "9999998", Message = "The request to the external service timed out." }
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetUserRepos :: FAILED :: Configuration error\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(500, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new() { Type = "CONFIGURATION_ERROR", Code = "9999997", Message = ex.Message }
                }
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "********************************************\n" +
                "GitHubController :: GetUserRepos :: FAILED :: Network error\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                transactionId);
            return StatusCode(502, new ErrorResponse
            {
                Errors = new List<ErrorDetail>
                {
                    new() { Type = "GATEWAY_ERROR", Code = "9999999", Message = "External service unavailable." }
                }
            });
        }
    }

    /// <summary>
    /// Processes the GitHub API response, mapping errors to appropriate HTTP status codes.
    /// </summary>
    private async Task<IActionResult> HandleExternalResponse(HttpResponseMessage response, string transactionId)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "********************************************\n" +
                "GitHubController :: HandleExternalResponse :: Returning SUCCESS response to caller\n" +
                "ResponseBody: {ResponseBody}\n" +
                "TransactionId: {TransactionId}\n" +
                "********************************************",
                content, transactionId);
            return Content(content, "application/json");
        }

        // Try to map GitHub error response
        if (ErrorCodeMapper.TryMapExternalError(content, response.StatusCode, out var errorResponse, out var statusCode))
        {
            return StatusCode((int)statusCode, errorResponse);
        }

        // Fallback for non-parseable error responses
        _logger.LogWarning("GitHub API returned {StatusCode} with unmapped response", (int)response.StatusCode);
        return StatusCode((int)response.StatusCode, new ErrorResponse
        {
            Errors = new List<ErrorDetail>
            {
                new()
                {
                    Type = "EXTERNAL_ERROR",
                    Code = ((int)response.StatusCode).ToString(),
                    Message = $"GitHub API returned status {(int)response.StatusCode}."
                }
            }
        });
    }
}