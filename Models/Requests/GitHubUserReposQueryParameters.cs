using Microsoft.AspNetCore.Mvc;

namespace GitHubClient.Models.Requests;

/// <summary>
/// Query parameters for the GitHub GET user repositories endpoint.
/// All parameters are optional — GitHub applies defaults when omitted.
/// </summary>
public class GitHubUserReposQueryParameters
{
    /// <summary>Sort field: created, updated, pushed, or full_name.</summary>
    [FromQuery(Name = "sort")]
    public string? Sort { get; set; }

    /// <summary>Number of results per page (1–100).</summary>
    [FromQuery(Name = "per_page")]
    public int? PerPage { get; set; }

    /// <summary>Page number of results to fetch (1-based).</summary>
    [FromQuery(Name = "page")]
    public int? Page { get; set; }
}