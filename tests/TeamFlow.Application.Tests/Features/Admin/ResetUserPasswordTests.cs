using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ResetUserPassword;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class ResetUserPasswordTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
        services.AddSingleton(_authService);
    }

    [Fact]
    public async Task Handle_SystemAdmin_ResetsPasswordSuccessfully()
    {
        var targetUser = UserBuilder.New().WithEmail("rup-target@example.com").Build();
        DbContext.Set<User>().Add(targetUser);
        await DbContext.SaveChangesAsync();

        _authService.HashPassword("NewPass@123").Returns("new-hashed-password");

        var result = await Sender.Send(new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SystemAdmin_HashesPassword()
    {
        var targetUser = UserBuilder.New().WithEmail("rup-hash@example.com").Build();
        DbContext.Set<User>().Add(targetUser);
        await DbContext.SaveChangesAsync();

        _authService.HashPassword("NewPass@123").Returns("new-hashed-password");

        await Sender.Send(new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123"));

        var updatedUser = await DbContext.Set<User>().FindAsync(targetUser.Id);
        updatedUser!.PasswordHash.Should().Be("new-hashed-password");
    }

    [Fact]
    public async Task Handle_SystemAdmin_SetsMustChangePasswordTrue()
    {
        var targetUser = UserBuilder.New().WithEmail("rup-mcp@example.com").WithMustChangePassword(false).Build();
        DbContext.Set<User>().Add(targetUser);
        await DbContext.SaveChangesAsync();

        _authService.HashPassword(Arg.Any<string>()).Returns("hashed");

        await Sender.Send(new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123"));

        var updatedUser = await DbContext.Set<User>().FindAsync(targetUser.Id);
        updatedUser!.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SystemAdmin_RevokesAllRefreshTokens()
    {
        var targetUser = UserBuilder.New().WithEmail("rup-revoke@example.com").Build();
        DbContext.Set<User>().Add(targetUser);
        await DbContext.SaveChangesAsync();

        DbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = targetUser.Id,
            TokenHash = "token-rup",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await DbContext.SaveChangesAsync();

        _authService.HashPassword(Arg.Any<string>()).Returns("hashed");

        await Sender.Send(new AdminResetUserPasswordCommand(targetUser.Id, "NewPass@123"));

        var activeTokens = await DbContext.Set<RefreshToken>()
            .Where(t => t.UserId == targetUser.Id && t.RevokedAt == null)
            .CountAsync();
        activeTokens.Should().Be(0);
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _authService.HashPassword(Arg.Any<string>()).Returns("hashed");
        var result = await Sender.Send(new AdminResetUserPasswordCommand(Guid.NewGuid(), "NewPass@123"));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("short")]
    public async Task Validator_InvalidPassword_FailsValidation(string? password)
    {
        var validator = new AdminResetUserPasswordValidator();
        var cmd = new AdminResetUserPasswordCommand(Guid.NewGuid(), password!);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ValidCommand_Passes()
    {
        var validator = new AdminResetUserPasswordValidator();
        var cmd = new AdminResetUserPasswordCommand(Guid.NewGuid(), "ValidPass@1");
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_EmptyUserId_FailsValidation()
    {
        var validator = new AdminResetUserPasswordValidator();
        var cmd = new AdminResetUserPasswordCommand(Guid.Empty, "ValidPass@1");
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}

[Collection("Auth")]
public sealed class ResetUserPasswordForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var authService = Substitute.For<IAuthService>();
        services.AddSingleton(authService);
    }

    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        var result = await Sender.Send(new AdminResetUserPasswordCommand(Guid.NewGuid(), "NewPass@123"));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
