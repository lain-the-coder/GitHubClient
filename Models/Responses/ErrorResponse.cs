using System.Text.Json.Serialization;

namespace GitHubClient.Models.Responses;

/// <summary>
/// Standardized error response envelope.
/// </summary>
public class ErrorResponse
{
    /// <summary>List of errors.</summary>
    [JsonPropertyName("errors")]
    public List<ErrorDetail> Errors { get; set; } = new();
}

/// <summary>
/// Individual error detail.
/// </summary>
public class ErrorDetail
{
    /// <summary>Error type/category.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Machine-readable error code.</summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable error message.</summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}