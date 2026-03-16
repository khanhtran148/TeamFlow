using FluentAssertions;
using TeamFlow.Application.Features.Retros.SubmitRetroCard;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class SubmitRetroCardValidatorTests
{
    private readonly SubmitRetroCardValidator _validator = new();

    [Fact]
    public async Task Validate_ValidInput_Passes()
    {
        var cmd = new SubmitRetroCardCommand(Guid.NewGuid(), RetroCardCategory.WentWell, "Great sprint!");
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(RetroCardCategory.WentWell)]
    [InlineData(RetroCardCategory.NeedsImprovement)]
    [InlineData(RetroCardCategory.ActionItem)]
    public async Task Validate_AllValidCategories_Pass(RetroCardCategory category)
    {
        var cmd = new SubmitRetroCardCommand(Guid.NewGuid(), category, "Some content");
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyContent_Fails(string? content)
    {
        var cmd = new SubmitRetroCardCommand(Guid.NewGuid(), RetroCardCategory.WentWell, content!);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [Fact]
    public async Task Validate_ContentExceedingMaxLength_Fails()
    {
        var longContent = new string('x', 2001);
        var cmd = new SubmitRetroCardCommand(Guid.NewGuid(), RetroCardCategory.WentWell, longContent);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
    }

    [Fact]
    public async Task Validate_ContentAtMaxLength_Passes()
    {
        var maxContent = new string('x', 2000);
        var cmd = new SubmitRetroCardCommand(Guid.NewGuid(), RetroCardCategory.WentWell, maxContent);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptySessionId_Fails()
    {
        var cmd = new SubmitRetroCardCommand(Guid.Empty, RetroCardCategory.WentWell, "Content");
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SessionId");
    }

    [Fact]
    public async Task Validate_InvalidCategoryEnumValue_Fails()
    {
        var cmd = new SubmitRetroCardCommand(Guid.NewGuid(), (RetroCardCategory)999, "Content");
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Category");
    }
}
