using GitHubClient.Models.Requests;
using FluentValidation;

namespace GitHubClient.Validators;

/// <summary>
/// Validates the GitHub username path parameter.
/// Rules: required, max 39 characters, alphanumeric + hyphens only, cannot start or end with hyphen.
/// </summary>
public class GitHubUsernameParametersValidator : AbstractValidator<GitHubUsernameParameters>
{
    /// <summary>
    /// Initializes validation rules for <see cref="GitHubUsernameParameters"/>.
    /// </summary>
    public GitHubUsernameParametersValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage("Username is required.")
            .MaximumLength(39)
            .WithMessage("Username must not exceed 39 characters.")
            .Matches(@"^[a-zA-Z0-9][a-zA-Z0-9-]*[a-zA-Z0-9]$|^[a-zA-Z0-9]$")
            .WithMessage("Username must contain only alphanumeric characters or hyphens, and cannot start or end with a hyphen.");
    }
}