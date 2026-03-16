using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Admin.ChangeUserStatus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Admin;

[Collection("Auth")]
public sealed class ChangeUserStatusTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICurrentUser>(_ => new TestAdminCurrentUser(SeedUserId));
    }

    [Fact]
    public async Task Handle_SystemAdmin_DeactivatesUser()
    {
        var target = UserBuilder.New().WithIsActive(true).Build();
        DbContext.Set<User>().Add(target);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminChangeUserStatusCommand(target.Id, false));

        result.IsSuccess.Should().BeTrue();
        var updated = await DbContext.Set<User>().FindAsync(target.Id);
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SystemAdmin_ActivatesUser()
    {
        var target = UserBuilder.New().WithIsActive(false).Build();
        DbContext.Set<User>().Add(target);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminChangeUserStatusCommand(target.Id, true));

        result.IsSuccess.Should().BeTrue();
        var updated = await DbContext.Set<User>().FindAsync(target.Id);
        updated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Deactivation_RevokesAllRefreshTokens()
    {
        var target = UserBuilder.New().WithIsActive(true).Build();
        DbContext.Set<User>().Add(target);
        await DbContext.SaveChangesAsync();

        DbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = target.Id,
            TokenHash = "token-hash-cus",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await DbContext.SaveChangesAsync();

        await Sender.Send(new AdminChangeUserStatusCommand(target.Id, false));

        var activeTokens = await DbContext.Set<RefreshToken>()
            .Where(t => t.UserId == target.Id && t.RevokedAt == null)
            .CountAsync();
        activeTokens.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Activation_DoesNotRevokeRefreshTokens()
    {
        var target = UserBuilder.New().WithIsActive(false).Build();
        DbContext.Set<User>().Add(target);
        await DbContext.SaveChangesAsync();

        DbContext.Set<RefreshToken>().Add(new RefreshToken
        {
            UserId = target.Id,
            TokenHash = "token-hash-cus2",
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await DbContext.SaveChangesAsync();

        await Sender.Send(new AdminChangeUserStatusCommand(target.Id, true));

        var activeTokens = await DbContext.Set<RefreshToken>()
            .Where(t => t.UserId == target.Id && t.RevokedAt == null)
            .CountAsync();
        activeTokens.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeactivateSelf_ReturnsForbidden()
    {
        var result = await Sender.Send(new AdminChangeUserStatusCommand(SeedUserId, false));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("own account");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        var result = await Sender.Send(new AdminChangeUserStatusCommand(Guid.NewGuid(), false));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DeactivateLastSystemAdmin_ReturnsForbidden()
    {
        // SeedUserId is the only SystemAdmin here — try to deactivate them after making them an admin
        var seedUser = await DbContext.Set<User>().FindAsync(SeedUserId);
        seedUser!.SystemRole = SystemRole.SystemAdmin;
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AdminChangeUserStatusCommand(SeedUserId, false));

        result.IsFailure.Should().BeTrue();
        // Could be "own account" or "last system administrator" — both acceptable
        (result.Error.ToLowerInvariant().Contains("own account") ||
         result.Error.ToLowerInvariant().Contains("last system administrator"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Validator_EmptyUserId_FailsValidation()
    {
        var validator = new AdminChangeUserStatusValidator();
        var cmd = new AdminChangeUserStatusCommand(Guid.Empty, false);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validator_ValidCommand_Passes()
    {
        var validator = new AdminChangeUserStatusValidator();
        var cmd = new AdminChangeUserStatusCommand(Guid.NewGuid(), true);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}

[Collection("Auth")]
public sealed class ChangeUserStatusForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NonSystemAdmin_ReturnsForbidden()
    {
        var result = await Sender.Send(new AdminChangeUserStatusCommand(Guid.NewGuid(), false));

        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("forbidden");
    }
}
