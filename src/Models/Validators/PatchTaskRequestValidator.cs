using FluentValidation;
using TaskPilot.Models.Tasks;

namespace TaskPilot.Models.Validators;

/// <summary>
/// Validates <see cref="PatchTaskRequest"/>. Rules fire only when the corresponding
/// field is supplied (non-null), mirroring the partial-update semantics of PATCH.
/// </summary>
public class PatchTaskRequestValidator : AbstractValidator<PatchTaskRequest>
{
    public PatchTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title must not be empty.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.TaskTypeId)
            .GreaterThan(0).WithMessage("TaskTypeId must be a valid type.")
            .When(x => x.TaskTypeId.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority value.")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid status value.")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Area)
            .IsInEnum().WithMessage("Invalid area value.")
            .When(x => x.Area.HasValue);

        RuleFor(x => x.TargetDateType)
            .IsInEnum().WithMessage("Invalid target date type.")
            .When(x => x.TargetDateType.HasValue);

        // TargetDate is required when TargetDateType is explicitly set to SpecificDay
        RuleFor(x => x.TargetDate)
            .NotNull().WithMessage("TargetDate is required when TargetDateType is SpecificDay.")
            .When(x => x.TargetDateType == Enums.TargetDateType.SpecificDay);

        // RecurrencePattern required+valid when IsRecurring is being set to true
        RuleFor(x => x.RecurrencePattern)
            .NotNull().WithMessage("RecurrencePattern is required when IsRecurring is true.")
            .IsInEnum().WithMessage("Invalid recurrence pattern.")
            .When(x => x.IsRecurring == true);
    }
}
