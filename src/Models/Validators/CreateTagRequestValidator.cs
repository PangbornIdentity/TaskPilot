using FluentValidation;
using TaskPilot.Models.Tags;

namespace TaskPilot.Models.Validators;

public class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    private static readonly System.Text.RegularExpressions.Regex HexColorRegex =
        new(@"^#[0-9A-Fa-f]{6}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(50).WithMessage("Tag name must not exceed 50 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .Matches(HexColorRegex).WithMessage("Color must be a valid hex value (e.g., #6255EC).");
    }
}
