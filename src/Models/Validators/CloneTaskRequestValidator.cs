using FluentValidation;
using TaskPilot.Models.Tasks;

namespace TaskPilot.Models.Validators;

/// <summary>
/// Validates <see cref="CloneTaskRequest"/>.
/// An empty body is valid. The only rule: when Title is non-null and non-whitespace,
/// it must not exceed 200 characters.
/// </summary>
public class CloneTaskRequestValidator : AbstractValidator<CloneTaskRequest>
{
    public CloneTaskRequestValidator()
    {
        RuleFor(r => r.Title)
            .MaximumLength(200)
            .WithMessage("Title must not exceed 200 characters.")
            .When(r => !string.IsNullOrWhiteSpace(r.Title));
    }
}
