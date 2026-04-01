using FluentValidation;
using TaskPilot.Models.Tasks;

namespace TaskPilot.Models.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.");

        RuleFor(x => x.Area)
            .IsInEnum().WithMessage("Invalid area value.");

        RuleFor(x => x.TargetDateType)
            .IsInEnum().WithMessage("Invalid target date type.");

        RuleFor(x => x.TargetDate)
            .NotNull().WithMessage("TargetDate is required when TargetDateType is SpecificDay.")
            .When(x => x.TargetDateType == Enums.TargetDateType.SpecificDay);

        RuleFor(x => x.RecurrencePattern)
            .NotNull().WithMessage("RecurrencePattern is required when IsRecurring is true.")
            .IsInEnum().WithMessage("Invalid recurrence pattern.")
            .When(x => x.IsRecurring);
    }
}
