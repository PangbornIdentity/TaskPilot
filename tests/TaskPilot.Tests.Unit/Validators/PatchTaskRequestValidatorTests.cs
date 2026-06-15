using TaskPilot.Models.Tasks;
using TaskPilot.Models.Enums;
using TaskPilot.Models.Validators;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Tests.Unit.Validators;

/// <summary>
/// Unit tests for the PatchTaskRequestValidator introduced with defect fix D-001.
/// Every test FAILS if the PatchTaskRequestValidator is removed or its rules are reverted.
/// REQ: FR-TASKS-008 (partial update), FR-TASKS-035 (validation).
/// </summary>
public class PatchTaskRequestValidatorTests
{
    private readonly PatchTaskRequestValidator _validator = new();

    // ── Baseline: an empty patch (no fields set) is always valid ─────────────

    [Fact]
    public void Validate_EmptyPatch_IsValid()
    {
        var request = new PatchTaskRequest();
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    // ── Title rules fire only when Title is supplied ──────────────────────────

    [Fact]
    public void Validate_TitleEmpty_HasError()
    {
        var request = new PatchTaskRequest(Title: "");
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_TitleWhitespaceOnly_HasError()
    {
        var request = new PatchTaskRequest(Title: "   ");
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_Title200Chars_IsValid()
    {
        var request = new PatchTaskRequest(Title: new string('A', 200));
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Title201Chars_HasError()
    {
        var request = new PatchTaskRequest(Title: new string('A', 201));
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_TitleNull_DoesNotProduceError()
    {
        // Null means "not supplied" in PATCH semantics — must not be validated.
        var request = new PatchTaskRequest(Title: null);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == "Title");
    }

    // ── IsRecurring=true requires a valid RecurrencePattern ──────────────────

    [Fact]
    public void Validate_IsRecurringTrueWithNullPattern_HasError()
    {
        var request = new PatchTaskRequest(IsRecurring: true, RecurrencePattern: null);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "RecurrencePattern");
    }

    [Fact]
    public void Validate_IsRecurringTrueWithValidPattern_IsValid()
    {
        var request = new PatchTaskRequest(IsRecurring: true, RecurrencePattern: RecurrencePattern.Weekly);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_IsRecurringFalseWithNullPattern_IsValid()
    {
        // False is a valid supplied value; no pattern required.
        var request = new PatchTaskRequest(IsRecurring: false, RecurrencePattern: null);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_IsRecurringNullWithNullPattern_IsValid()
    {
        // Neither field supplied — pure absence, always valid for PATCH.
        var request = new PatchTaskRequest(IsRecurring: null, RecurrencePattern: null);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    // ── Valid partial patches (single field each) ─────────────────────────────

    [Theory]
    [InlineData(TaskPriority.Low)]
    [InlineData(TaskPriority.Medium)]
    [InlineData(TaskPriority.High)]
    [InlineData(TaskPriority.Critical)]
    public void Validate_PriorityAlone_IsValid(TaskPriority priority)
    {
        var request = new PatchTaskRequest(Priority: priority);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(TaskStatus.NotStarted)]
    [InlineData(TaskStatus.InProgress)]
    [InlineData(TaskStatus.Blocked)]
    [InlineData(TaskStatus.Completed)]
    [InlineData(TaskStatus.Cancelled)]
    public void Validate_StatusAlone_IsValid(TaskStatus status)
    {
        var request = new PatchTaskRequest(Status: status);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(Area.Personal)]
    [InlineData(Area.Work)]
    public void Validate_AreaAlone_IsValid(Area area)
    {
        var request = new PatchTaskRequest(Area: area);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    // ── TargetDateType=SpecificDay requires TargetDate ───────────────────────

    [Fact]
    public void Validate_SpecificDayWithoutDate_HasError()
    {
        var request = new PatchTaskRequest(
            TargetDateType: TargetDateType.SpecificDay,
            TargetDate: null);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TargetDate");
    }

    [Fact]
    public void Validate_SpecificDayWithDate_IsValid()
    {
        var request = new PatchTaskRequest(
            TargetDateType: TargetDateType.SpecificDay,
            TargetDate: DateTime.UtcNow.AddDays(1));
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    // ── TaskTypeId validation (fires only when HasValue) ─────────────────────

    [Fact]
    public void Validate_TaskTypeIdZero_HasError()
    {
        var request = new PatchTaskRequest(TaskTypeId: 0);
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TaskTypeId");
    }

    [Fact]
    public void Validate_TaskTypeIdOne_IsValid()
    {
        var request = new PatchTaskRequest(TaskTypeId: 1);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_TaskTypeIdNull_IsValid()
    {
        // Null = not supplied in PATCH; must not trigger the GreaterThan(0) rule.
        var request = new PatchTaskRequest(TaskTypeId: null);
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }
}
