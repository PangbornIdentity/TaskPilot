using TaskPilot.Models.Tasks;
using TaskPilot.Models.Enums;
using TaskPilot.Models.Validators;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Validators;

public class UpdateTaskRequestValidatorTests
{
    private readonly UpdateTaskRequestValidator _validator = new();

    private static UpdateTaskRequest ValidRequest() => new(
        Title: "Valid Task",
        Description: null,
        TaskTypeId: null,
        Area: Area.Personal,
        Priority: TaskPriority.Medium,
        Status: TaskStatus.NotStarted,
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
    public void Validate_TitleMaxLength_IsValid()
    {
        var request = ValidRequest() with { Title = new string('A', 200) };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(Area.Personal)]
    [InlineData(Area.Work)]
    public void Validate_ValidAreas_AreAccepted(Area area)
    {
        var request = ValidRequest() with { Area = area };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
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
    public void Validate_SpecificDayWithDate_IsValid()
    {
        var request = ValidRequest() with
        {
            TargetDateType = TargetDateType.SpecificDay,
            TargetDate = DateTime.UtcNow.AddDays(1)
        };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
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

    [Fact]
    public void Validate_RecurringWithPattern_IsValid()
    {
        var request = ValidRequest() with
        {
            IsRecurring = true,
            RecurrencePattern = RecurrencePattern.Daily
        };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NotRecurringWithNoPattern_IsValid()
    {
        var request = ValidRequest() with { IsRecurring = false, RecurrencePattern = null };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AllValidStatuses_AreAccepted()
    {
        foreach (var status in Enum.GetValues<TaskStatus>())
        {
            var request = ValidRequest() with { Status = status };
            var result = _validator.Validate(request);
            Assert.True(result.IsValid, $"Status {status} should be valid");
        }
    }
}
