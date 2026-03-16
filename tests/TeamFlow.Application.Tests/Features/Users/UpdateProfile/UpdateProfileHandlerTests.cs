using FluentAssertions;
using TeamFlow.Application.Features.Users.UpdateProfile;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Users.UpdateProfile;

[Collection("Auth")]
public sealed class UpdateProfileHandlerTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_UpdatesNameAndAvatar()
    {
        var cmd = new UpdateProfileCommand("New Name", "https://example.com/avatar.jpg");

        await Sender.Send(cmd);

        var user = await DbContext.Set<User>().FindAsync(SeedUserId);
        user!.Name.Should().Be("New Name");
        user.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsUpdatedProfile()
    {
        var cmd = new UpdateProfileCommand("Updated Name", "https://example.com/avatar.jpg");

        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Name");
        result.Value.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
    }

    [Fact]
    public async Task Handle_NullAvatarUrl_ClearsAvatar()
    {
        // First set an avatar
        var user = await DbContext.Set<User>().FindAsync(SeedUserId);
        user!.AvatarUrl = "https://example.com/old-avatar.jpg";
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var cmd = new UpdateProfileCommand("Same Name", null);
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.AvatarUrl.Should().BeNull();

        var updated = await DbContext.Set<User>().FindAsync(SeedUserId);
        updated!.AvatarUrl.Should().BeNull();
    }
}
