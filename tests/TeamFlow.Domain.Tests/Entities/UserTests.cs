using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Domain.Tests.Entities;

public sealed class UserTests
{
    [Fact]
    public void User_MustChangePassword_DefaultsToFalse()
    {
        var user = UserBuilder.New().Build();

        user.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public void User_IsActive_DefaultsToTrue()
    {
        var user = UserBuilder.New().Build();

        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void User_WithMustChangePasswordTrue_ShouldRetain()
    {
        var user = UserBuilder.New().WithMustChangePassword(true).Build();

        user.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public void User_WithIsActiveFalse_ShouldRetain()
    {
        var user = UserBuilder.New().WithIsActive(false).Build();

        user.IsActive.Should().BeFalse();
    }
}
