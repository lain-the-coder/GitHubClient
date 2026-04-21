using System.Net;
using System.Text.Json;
using GitHubClient.Models.Responses;

namespace GitHubClient.Helpers;

/// <summary>
/// Maps GitHub API HTTP status codes to appropriate responses.
/// </summary>
public static class ErrorCodeMapper
{
    private static readonly Dictionary<HttpStatusCode, HttpStatusCode> StatusCodeMap = new()
    {
        [HttpStatusCode.Unauthorized] = HttpStatusCode.Unauthorized,
        [HttpStatusCode.Forbidden] = HttpStatusCode.Forbidden,
        [HttpStatusCode.NotFound] = HttpStatusCode.NotFound,
        [HttpStatusCode.UnprocessableEntity] = HttpStatusCode.BadRequest,
        [HttpStatusCode.InternalServerError] = HttpStatusCode.InternalServerError,
        [HttpStatusCode.ServiceUnavailable] = HttpStatusCode.BadGateway
    };

    /// <summary>
    /// Maps a GitHub API HTTP status code to the corresponding proxy status code.
    /// Returns 502 Bad Gateway if the status code is not recognized.
    /// </summary>
    /// <param name="externalStatusCode">HTTP status code from the GitHub API.</param>
    /// <returns>Mapped HTTP status code.</returns>
    public static HttpStatusCode MapStatusCode(HttpStatusCode externalStatusCode)
    {
        if (StatusCodeMap.TryGetValue(externalStatusCode, out var mappedCode))
        {
            return mappedCode;
        }

        return HttpStatusCode.BadGateway;
    }

    /// <summary>
    /// Attempts to parse a GitHub API error response and return a mapped ErrorResponse with status code.
    /// GitHub errors use { "message": "...", "documentation_url": "..." } format.
    /// </summary>
    /// <param name="responseContent">JSON response content from the GitHub API.</param>
    /// <param name="externalStatusCode">HTTP status code from the GitHub API.</param>
    /// <param name="errorResponse">Parsed error response.</param>
    /// <param name="statusCode">Mapped HTTP status code.</param>
    /// <returns>True if the error was parsed and mapped; false otherwise.</returns>
    public static bool TryMapExternalError(
        string responseContent,
        HttpStatusCode externalStatusCode,
        out ErrorResponse errorResponse,
        out HttpStatusCode statusCode)
    {
        errorResponse = new ErrorResponse();
        statusCode = HttpStatusCode.BadGateway;

        try
        {
            var externalError = JsonSerializer.Deserialize<ExternalApiErrorResponse>(responseContent);
            if (externalError != null && !string.IsNullOrEmpty(externalError.Message))
            {
                statusCode = MapStatusCode(externalStatusCode);

                errorResponse.Errors = new List<ErrorDetail>
                {
                    new()
                    {
                        Type = "EXTERNAL_ERROR",
                        Code = ((int)externalStatusCode).ToString(),
                        Message = externalError.Message
                    }
                };

                return true;
            }
        }
        catch (JsonException)
        {
            // Response is not valid JSON — treat as opaque external error
        }

        return false;
    }
}