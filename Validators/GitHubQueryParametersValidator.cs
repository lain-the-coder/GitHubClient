using GitHubClient.Models.Requests;
using FluentValidation;

namespace GitHubClient.Validators;

/// <summary>
/// Validates GET GitHub query parameters.
/// Rules will be added as query parameters are introduced.
/// </summary>
public class GitHubQueryParametersValidator : AbstractValidator<GitHubQueryParameters>
{
    /// <summary>
    /// Initializes validation rules for <see cref="GitHubQueryParameters"/>.
    /// </summary>
    public GitHubQueryParametersValidator()
    {
    }
}