using GitHubClient.Models.Requests;
using FluentValidation;

namespace GitHubClient.Validators;

/// <summary>
/// Validates the POST GitHub request body.
/// Rules will be added when POST operations are introduced.
/// </summary>
public class GitHubRequestValidator : AbstractValidator<GitHubUserRequest>
{
    /// <summary>
    /// Initializes validation rules for <see cref="GitHubUserRequest"/>.
    /// </summary>
    public GitHubRequestValidator()
    {
    }
}