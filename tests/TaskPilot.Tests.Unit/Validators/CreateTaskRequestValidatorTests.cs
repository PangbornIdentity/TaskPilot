using TaskPilot.Shared.DTOs.Tasks;
using TaskPilot.Shared.Enums;
using TaskPilot.Shared.Validators;

namespace TaskPilot.Tests.Unit.Validators;

public class CreateTaskRequestValidatorTests
{
    private readonly CreateTaskRequestValidator _validator = new();

    private static CreateTaskRequest ValidRequest() => new(
        Title: "Valid Task",
        Description: null,
        Type: "Work",
        Priority: TaskPriority.Medium,
        Status: Shared.Enums.TaskStatus.NotStarted,
        TargetDateType: TargetDateType.ThisWeek,
        TargetDate: null,
        IsRecurring: false,
        RecurrencePattern: null,
        TagIds: null
    );

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var result = _validator.Validate(ValidRequest());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTitle_HasError()
    {
        var request = ValidRequest() with { Title = string.Empty };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_TitleTooLong_HasError()
    {
        var request = ValidRequest() with { Title = new string('A', 201) };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_InvalidType_HasError()
    {
        var request = ValidRequest() with { Type = "InvalidType" };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Type");
    }

    [Fact]
    public void Validate_SpecificDayWithNoDate_HasError()
    {
        var request = ValidRequest() with
        {
            TargetDateType = TargetDateType.SpecificDay,
            TargetDate = null
        };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TargetDate");
    }

    [Fact]
    public void Validate_RecurringWithNoPattern_HasError()
    {
        var request = ValidRequest() with
        {
            IsRecurring = true,
            RecurrencePattern = null
        };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "RecurrencePattern");
    }
}
