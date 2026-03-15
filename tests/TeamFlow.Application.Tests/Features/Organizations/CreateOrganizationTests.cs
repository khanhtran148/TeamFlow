using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Organizations.CreateOrganization;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Organizations;

public sealed class CreateOrganizationTests
{
    private readonly IOrganizationRepository _orgRepo = Substitute.For<IOrganizationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public CreateOrganizationTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _orgRepo.AddAsync(Arg.Any<Organization>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Organization>());
    }

    private CreateOrganizationHandler CreateHandler() => new(_orgRepo, _currentUser);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithDto()
    {
        var cmd = new CreateOrganizationCommand("My Org");

        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Org");
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsOrganization()
    {
        var cmd = new CreateOrganizationCommand("My Org");

        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _orgRepo.Received(1).AddAsync(
            Arg.Is<Organization>(o => o.Name == "My Org" && o.CreatedByUserId == _currentUser.Id),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyName_ReturnsValidationError(string? name)
    {
        var validator = new CreateOrganizationValidator();
        var cmd = new CreateOrganizationCommand(name!);

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateOrganizationCommand.Name));
    }

    [Fact]
    public async Task Validate_NameTooLong_ReturnsValidationError()
    {
        var validator = new CreateOrganizationValidator();
        var cmd = new CreateOrganizationCommand(new string('A', 201));

        var validationResult = await validator.ValidateAsync(cmd);

        validationResult.IsValid.Should().BeFalse();
    }
}
