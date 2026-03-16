using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Auth.Register;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Auth;

[Collection("Auth")]
public sealed class RegisterTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed-password");
        _authService.GenerateJwt(Arg.Any<User>()).Returns("jwt-token");
        _authService.GenerateRefreshToken().Returns("refresh-token");
        _authService.HashToken(Arg.Any<string>()).Returns("hashed-refresh-token");
        services.AddSingleton(_authService);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTokens()
    {
        var cmd = new RegisterCommand("register-valid@test.com", "Password1", "Test User");

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        result.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsUser()
    {
        var cmd = new RegisterCommand("register-persist@test.com", "Password1", "Test User");

        await Sender.Send(cmd);

        var exists = await DbContext.Set<User>().AnyAsync(u => u.Email == "register-persist@test.com");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_NormalizesEmailToLowerCase()
    {
        var cmd = new RegisterCommand("REGISTER-UPPER@TEST.COM", "Password1", "Test User");

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        var exists = await DbContext.Set<User>().AnyAsync(u => u.Email == "register-upper@test.com");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictError()
    {
        // Use the already-seeded user email to force a conflict
        var cmd = new RegisterCommand("test@teamflow.dev", "Password1", "Test User");

        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Theory]
    [InlineData("", "Password1", "Name")]
    [InlineData("not-an-email", "Password1", "Name")]
    [InlineData("user@test.com", "", "Name")]
    [InlineData("user@test.com", "short", "Name")]
    [InlineData("user@test.com", "nouppercase1", "Name")]
    [InlineData("user@test.com", "NOLOWERCASE1", "Name")]
    [InlineData("user@test.com", "NoDigits!", "Name")]
    [InlineData("user@test.com", "Password1", "")]
    public async Task Handle_InvalidInput_FailsValidation(string email, string password, string name)
    {
        var validator = new RegisterValidator();
        var cmd = new RegisterCommand(email, password, name);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
