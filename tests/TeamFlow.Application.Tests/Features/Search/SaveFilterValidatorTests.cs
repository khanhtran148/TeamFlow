using System.Text.Json;
using FluentAssertions;
using TeamFlow.Application.Features.Search.SaveFilter;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class SaveFilterValidatorTests
{
    private readonly SaveFilterValidator _validator = new();

    [Fact]
    public async Task Validate_ValidInput_Passes()
    {
        var cmd = new SaveFilterCommand(Guid.NewGuid(), "My Filter", JsonDocument.Parse("{}"), false);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyProjectId_Fails()
    {
        var cmd = new SaveFilterCommand(Guid.Empty, "My Filter", JsonDocument.Parse("{}"), false);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyOrNullName_Fails(string? name)
    {
        var cmd = new SaveFilterCommand(Guid.NewGuid(), name!, JsonDocument.Parse("{}"), false);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NameExceedingMaxLength_Fails()
    {
        var longName = new string('x', 101);
        var cmd = new SaveFilterCommand(Guid.NewGuid(), longName, JsonDocument.Parse("{}"), false);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NameAtMaxLength_Passes()
    {
        var maxName = new string('x', 100);
        var cmd = new SaveFilterCommand(Guid.NewGuid(), maxName, JsonDocument.Parse("{}"), false);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NullFilterJson_Fails()
    {
        var cmd = new SaveFilterCommand(Guid.NewGuid(), "My Filter", null!, false);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FilterJson");
    }
}
