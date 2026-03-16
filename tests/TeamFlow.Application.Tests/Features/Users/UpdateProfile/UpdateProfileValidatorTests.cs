using FluentAssertions;
using TeamFlow.Application.Features.Users.UpdateProfile;

namespace TeamFlow.Application.Tests.Features.Users.UpdateProfile;

public sealed class UpdateProfileValidatorTests
{
    private readonly UpdateProfileValidator _validator = new();

    [Fact]
    public async Task Validate_ValidCommand_NoErrors()
    {
        var cmd = new UpdateProfileCommand("Jane Doe", "https://example.com/avatar.jpg");
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ValidCommandWithNullAvatar_NoErrors()
    {
        var cmd = new UpdateProfileCommand("Jane Doe", null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsError(string? name)
    {
        var cmd = new UpdateProfileCommand(name!, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsError()
    {
        var longName = new string('A', 101);
        var cmd = new UpdateProfileCommand(longName, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_AvatarUrlTooLong_ReturnsError()
    {
        var longUrl = "https://example.com/" + new string('a', 2048);
        var cmd = new UpdateProfileCommand("Valid Name", longUrl);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AvatarUrl");
    }

    [Fact]
    public async Task Validate_NameExactly100Chars_IsValid()
    {
        var name = new string('A', 100);
        var cmd = new UpdateProfileCommand(name, null);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
