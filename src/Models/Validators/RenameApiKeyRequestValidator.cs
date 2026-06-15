using FluentValidation;
using TaskPilot.Models.ApiKeys;

namespace TaskPilot.Models.Validators;

/// <summary>
/// Validates <see cref="RenameApiKeyRequest"/>. Mirrors the name rules from
/// <see cref="CreateApiKeyRequestValidator"/>: non-empty, max 100 characters.
/// </summary>
public class RenameApiKeyRequestValidator : AbstractValidator<RenameApiKeyRequest>
{
    public RenameApiKeyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key name is required.")
            .MaximumLength(100).WithMessage("API key name must not exceed 100 characters.");
    }
}
