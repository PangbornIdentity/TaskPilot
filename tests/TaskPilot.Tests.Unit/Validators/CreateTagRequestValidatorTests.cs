using TaskPilot.Shared.DTOs.Tags;
using TaskPilot.Shared.Validators;

namespace TaskPilot.Tests.Unit.Validators;

public class CreateTagRequestValidatorTests
{
    private readonly CreateTagRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var result = _validator.Validate(new CreateTagRequest("Work", "#6255EC"));
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var result = _validator.Validate(new CreateTagRequest(string.Empty, "#6255EC"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_InvalidHexColor_HasError()
    {
        var result = _validator.Validate(new CreateTagRequest("Work", "not-a-color"));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Color");
    }

    [Fact]
    public void Validate_ValidHexColor_IsValid()
    {
        var result1 = _validator.Validate(new CreateTagRequest("Tag", "#AABBCC"));
        var result2 = _validator.Validate(new CreateTagRequest("Tag", "#aabbcc"));
        var result3 = _validator.Validate(new CreateTagRequest("Tag", "#000000"));
        var result4 = _validator.Validate(new CreateTagRequest("Tag", "#FFFFFF"));

        Assert.True(result1.IsValid);
        Assert.True(result2.IsValid);
        Assert.True(result3.IsValid);
        Assert.True(result4.IsValid);
    }
}
