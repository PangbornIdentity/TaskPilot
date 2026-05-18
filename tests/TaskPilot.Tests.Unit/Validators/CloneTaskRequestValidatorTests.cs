using TaskPilot.Models.Tasks;
using TaskPilot.Models.Validators;

namespace TaskPilot.Tests.Unit.Validators;

/// <summary>Tests for CloneTaskRequestValidator (U-CL-V-001 to U-CL-V-010).</summary>
public class CloneTaskRequestValidatorTests
{
    private readonly CloneTaskRequestValidator _validator = new();

    [Fact]
    public async Task Validate_EmptyRequest_Passes()
    {
        var result = await _validator.ValidateAsync(new CloneTaskRequest());
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyBody_Passes()
    {
        // Simulates {} deserialized — all properties are default
        var request = new CloneTaskRequest(null, null, false);
        var result = await _validator.ValidateAsync(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_TitleNull_Passes()
    {
        var result = await _validator.ValidateAsync(new CloneTaskRequest(Title: null));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_TitleWhitespaceOnly_Passes()
    {
        var result = await _validator.ValidateAsync(new CloneTaskRequest(Title: "   "));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_TitleExactly200Chars_Passes()
    {
        var title = new string('a', 200);
        var result = await _validator.ValidateAsync(new CloneTaskRequest(Title: title));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_Title201Chars_FailsWithMessage()
    {
        var title = new string('a', 201);
        var result = await _validator.ValidateAsync(new CloneTaskRequest(Title: title));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title" && e.ErrorMessage.Contains("200"));
    }

    [Fact]
    public async Task Validate_ClearTargetDateFalse_Passes()
    {
        var result = await _validator.ValidateAsync(new CloneTaskRequest(ClearTargetDate: false));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ClearTargetDateTrue_Passes()
    {
        var result = await _validator.ValidateAsync(new CloneTaskRequest(ClearTargetDate: true));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_TargetDateSupplied_ClearFalse_Passes()
    {
        var result = await _validator.ValidateAsync(new CloneTaskRequest(TargetDate: DateTime.UtcNow.AddDays(7), ClearTargetDate: false));
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_TargetDateSupplied_ClearTrue_Passes()
    {
        var result = await _validator.ValidateAsync(new CloneTaskRequest(TargetDate: DateTime.UtcNow, ClearTargetDate: true));
        Assert.True(result.IsValid);
    }
}
