using GitHubClient.Models.Requests;
using FluentValidation;

namespace GitHubClient.Validators;

/// <summary>
/// Validates the GitHub create repository request body.
/// Rules: name is required, max 100 characters, must match GitHub repo naming rules.
/// Description when provided must not exceed 350 characters.
/// </summary>
public class GitHubCreateRepoRequestValidator : AbstractValidator<GitHubCreateRepoRequest>
{
    /// <summary>
    /// Initializes validation rules for <see cref="GitHubCreateRepoRequest"/>.
    /// </summary>
    public GitHubCreateRepoRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9_][a-zA-Z0-9._-]*$")
            .WithMessage("Name must contain only alphanumeric characters, hyphens, underscores, or dots, and cannot start with a dot.");

        When(x => !string.IsNullOrEmpty(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(350)
                .WithMessage("Description must not exceed 350 characters.");
        });
    }
}