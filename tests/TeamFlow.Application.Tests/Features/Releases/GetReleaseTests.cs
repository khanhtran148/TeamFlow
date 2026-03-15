using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.GetRelease;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class GetReleaseTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();

    private GetReleaseHandler CreateHandler() => new(_releaseRepo);

    [Fact]
    public async Task Handle_ExistingRelease_ReturnsReleaseDto()
    {
        var release = ReleaseBuilder.New().WithName("v1.0.0").WithDescription("First release").Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _releaseRepo.GetItemStatusCountsAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<WorkItemStatus, int> { [WorkItemStatus.Done] = 5 });

        var result = await CreateHandler().Handle(new GetReleaseQuery(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("v1.0.0");
        result.Value.ItemCountsByStatus.Should().ContainKey("Done");
    }

    [Fact]
    public async Task Handle_NonExistentRelease_ReturnsFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await CreateHandler().Handle(new GetReleaseQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
