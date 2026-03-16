using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.ChangePassword;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Auth;

[Collection("Auth")]
public sealed class ChangePasswordTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _authService.HashPassword("NewPassword1").Returns("new-hashed-password");
        services.AddSingleton(_authService);
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_ChangesSuccessfully()
    {
        // SeedUserId user already exists — update password hash
        var seedUser = await DbContext.Set<User>().FindAsync(SeedUserId);
        seedUser!.PasswordHash = "old-hash";
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("OldPassword1", "old-hash").Returns(true);

        var cmd = new ChangePasswordCommand("OldPassword1", "NewPassword1");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        var updatedUser = await DbContext.Set<User>().FindAsync(SeedUserId);
        updatedUser!.PasswordHash.Should().Be("new-hashed-password");
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsFailure()
    {
        var seedUser = await DbContext.Set<User>().FindAsync(SeedUserId);
        seedUser!.PasswordHash = "old-hash";
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("wrong", "old-hash").Returns(false);

        var cmd = new ChangePasswordCommand("wrong", "NewPassword1");
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("incorrect");
    }

    [Fact]
    public async Task Handle_UserWithMustChangePassword_ClearsFlag()
    {
        var seedUser = await DbContext.Set<User>().FindAsync(SeedUserId);
        seedUser!.PasswordHash = "old-hash";
        seedUser.MustChangePassword = true;
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("OldPassword1", "old-hash").Returns(true);

        var cmd = new ChangePasswordCommand("OldPassword1", "NewPassword1");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        var updatedUser = await DbContext.Set<User>().FindAsync(SeedUserId);
        updatedUser!.MustChangePassword.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "NewPassword1")]
    [InlineData("OldPassword1", "")]
    [InlineData("OldPassword1", "short")]
    [InlineData("OldPassword1", "nouppercase1")]
    public async Task Handle_InvalidInput_FailsValidation(string currentPassword, string newPassword)
    {
        var validator = new ChangePasswordValidator();
        var cmd = new ChangePasswordCommand(currentPassword, newPassword);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
