using FluentValidation;
using TaskPilot.Models.ApiKeys;

namespace TaskPilot.Models.Validators;

public class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("API key name is required.")
            .MaximumLength(100).WithMessage("API key name must not exceed 100 characters.");
    }
}
