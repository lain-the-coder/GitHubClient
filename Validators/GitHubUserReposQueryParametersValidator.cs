using GitHubClient.Models.Requests;
using FluentValidation;

namespace GitHubClient.Validators;

/// <summary>
/// Validates the GitHub user repositories query parameters.
/// Rules: sort must be one of (created, updated, pushed, full_name), per_page between 1–100, page >= 1.
/// All rules use When guards — only validate when the parameter is provided.
/// </summary>
public class GitHubUserReposQueryParametersValidator : AbstractValidator<GitHubUserReposQueryParameters>
{
    private static readonly string[] AllowedSortValues = { "created", "updated", "pushed", "full_name" };

    /// <summary>
    /// Initializes validation rules for <see cref="GitHubUserReposQueryParameters"/>.
    /// </summary>
    public GitHubUserReposQueryParametersValidator()
    {
        When(x => !string.IsNullOrEmpty(x.Sort), () =>
        {
            RuleFor(x => x.Sort)
                .Must(val => AllowedSortValues.Contains(val, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Sort must be one of: created, updated, pushed, full_name.");
        });

        When(x => x.PerPage.HasValue, () =>
        {
            RuleFor(x => x.PerPage!.Value)
                .InclusiveBetween(1, 100)
                .WithMessage("PerPage must be between 1 and 100.");
        });

        When(x => x.Page.HasValue, () =>
        {
            RuleFor(x => x.Page!.Value)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be greater than or equal to 1.");
        });
    }
}