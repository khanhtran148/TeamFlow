using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.Login;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Auth;

[Collection("Auth")]
public sealed class LoginTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _authService.GenerateJwt(Arg.Any<User>()).Returns("jwt-token");
        _authService.GenerateRefreshToken().Returns("refresh-token");
        _authService.HashToken(Arg.Any<string>()).Returns("hashed-refresh-token");
        services.AddSingleton(_authService);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        var user = UserBuilder.New()
            .WithEmail("login-valid@test.com")
            .WithPasswordHash("hashed-password")
            .Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);

        var result = await Sender.Send(new LoginCommand("login-valid@test.com", "Password1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsFailure()
    {
        var user = UserBuilder.New()
            .WithEmail("login-wrong@test.com")
            .WithPasswordHash("hashed-password")
            .Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("wrong", "hashed-password").Returns(false);

        var result = await Sender.Send(new LoginCommand("login-wrong@test.com", "wrong"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_NonExistentEmail_ReturnsFailure()
    {
        var result = await Sender.Send(new LoginCommand("nobody@test.com", "Password1"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid");
    }

    [Fact]
    public async Task Handle_UserWithMustChangePassword_ReturnsFlag()
    {
        var user = UserBuilder.New()
            .WithEmail("login-mcp@test.com")
            .WithPasswordHash("hashed-password")
            .WithMustChangePassword(true)
            .Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);

        var result = await Sender.Send(new LoginCommand("login-mcp@test.com", "Password1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UserWithoutMustChangePassword_ReturnsFlagFalse()
    {
        var user = UserBuilder.New()
            .WithEmail("login-nomcp@test.com")
            .WithPasswordHash("hashed-password")
            .WithMustChangePassword(false)
            .Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);

        var result = await Sender.Send(new LoginCommand("login-nomcp@test.com", "Password1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsForbidden()
    {
        var user = UserBuilder.New()
            .WithEmail("login-deactivated@test.com")
            .WithPasswordHash("hashed-password")
            .WithIsActive(false)
            .Build();
        DbContext.Set<User>().Add(user);
        await DbContext.SaveChangesAsync();

        _authService.VerifyPassword("Password1", "hashed-password").Returns(true);

        var result = await Sender.Send(new LoginCommand("login-deactivated@test.com", "Password1"));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("deactivated");
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("user@test.com", "")]
    public async Task Handle_EmptyFields_FailsValidation(string email, string password)
    {
        var validator = new LoginValidator();
        var cmd = new LoginCommand(email, password);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
