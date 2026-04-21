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
    private readonly ILogger<GitHubController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GitHubController"/>.
    /// </summary>
    public GitHubController(
        IGitHubService gitHubService,
        IValidator<GitHubQueryParameters> getValidator,
        ILogger<GitHubController> logger)
    {
        _gitHubService = gitHubService;
        _getValidator = getValidator;
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