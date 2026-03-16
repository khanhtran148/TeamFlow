using FluentAssertions;
using TeamFlow.Application.Features.Search.UpdateSavedFilter;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class UpdateSavedFilterValidatorTests
{
    private readonly UpdateSavedFilterValidator _validator = new();

    [Fact]
    public async Task Validate_ValidInput_Passes()
    {
        var cmd = new UpdateSavedFilterCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated Name", null, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyProjectId_Fails()
    {
        var cmd = new UpdateSavedFilterCommand(Guid.Empty, Guid.NewGuid(), "Name", null, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    [Fact]
    public async Task Validate_EmptyFilterId_Fails()
    {
        var cmd = new UpdateSavedFilterCommand(Guid.NewGuid(), Guid.Empty, "Name", null, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FilterId");
    }

    [Fact]
    public async Task Validate_NameExceedingMaxLength_Fails()
    {
        var longName = new string('x', 101);
        var cmd = new UpdateSavedFilterCommand(Guid.NewGuid(), Guid.NewGuid(), longName, null, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NameAtMaxLength_Passes()
    {
        var maxName = new string('x', 100);
        var cmd = new UpdateSavedFilterCommand(Guid.NewGuid(), Guid.NewGuid(), maxName, null, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NullName_SkipsLengthValidation()
    {
        var cmd = new UpdateSavedFilterCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
