using TaskPilot.Models.Tags;
using TaskPilot.Models.Validators;

namespace TaskPilot.Tests.Unit.Validators;

public class UpdateTagRequestValidatorTests
{
    private readonly UpdateTagRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var result = _validator.Validate(new UpdateTagRequest("Work", "#6255EC"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var result = _validator.Validate(new UpdateTagRequest(string.Empty, "#6255EC"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameOver50Chars_HasError()
    {
        var name = new string('a', 51);
        var result = _validator.Validate(new UpdateTagRequest(name, "#6255EC"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_InvalidHexColor_HasError()
    {
        var result = _validator.Validate(new UpdateTagRequest("Work", "not-a-color"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Color");
    }

    [Fact]
    public void Validate_ValidHexColor_IsValid()
    {
        var lower = _validator.Validate(new UpdateTagRequest("Tag", "#aabbcc"));
        var upper = _validator.Validate(new UpdateTagRequest("Tag", "#AABBCC"));
        Assert.True(lower.IsValid);
        Assert.True(upper.IsValid);
    }
}
