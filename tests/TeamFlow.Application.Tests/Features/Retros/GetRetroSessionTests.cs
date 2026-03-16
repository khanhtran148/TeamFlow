using FluentAssertions;
using TeamFlow.Application.Features.Retros.GetRetroSession;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class GetRetroSessionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task GetSession_Anonymous_StripsAuthorInfo()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .WithStatus(RetroSessionStatus.Open)
            .Anonymous()
            .Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var cardAuthor = UserBuilder.New().WithEmail("retro-anon-author@example.com").Build();
        DbContext.Users.Add(cardAuthor);
        await DbContext.SaveChangesAsync();

        var card = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = cardAuthor.Id,
            Category = RetroCardCategory.WentWell,
            Content = "Good work"
        };
        DbContext.Set<RetroCard>().Add(card);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetRetroSessionQuery(session.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Cards.Should().HaveCount(1);
        result.Value.Cards[0].AuthorId.Should().BeNull();
        result.Value.Cards[0].AuthorName.Should().BeNull();
    }

    [Fact]
    public async Task GetSession_Public_IncludesAuthorInfo()
    {
        var project = await SeedProjectAsync();
        var session = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .WithStatus(RetroSessionStatus.Open)
            .Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var card = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.WentWell,
            Content = "Good work"
        };
        DbContext.Set<RetroCard>().Add(card);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetRetroSessionQuery(session.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Cards[0].AuthorId.Should().Be(SeedUserId);
        result.Value.Cards[0].AuthorName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetSession_NotFound_ReturnsFailure()
    {
        var result = await Sender.Send(new GetRetroSessionQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
    }
}
