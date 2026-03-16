using FluentAssertions;
using TeamFlow.Application.Features.Search.FullTextSearch;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class FullTextSearchValidatorTests
{
    private readonly FullTextSearchValidator _validator = new();

    [Fact]
    public async Task Validate_ValidInput_Passes()
    {
        var query = new FullTextSearchQuery(Guid.NewGuid(), "search term", null, null, null, null, null, null, null, null, 1, 20);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_EmptyProjectId_Fails()
    {
        var query = new FullTextSearchQuery(Guid.Empty, "search", null, null, null, null, null, null, null, null, 1, 20);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProjectId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_InvalidPage_Fails(int page)
    {
        var query = new FullTextSearchQuery(Guid.NewGuid(), null, null, null, null, null, null, null, null, null, page, 20);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public async Task Validate_PageOne_Passes()
    {
        var query = new FullTextSearchQuery(Guid.NewGuid(), null, null, null, null, null, null, null, null, null, 1, 20);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Validate_InvalidPageSize_Fails(int pageSize)
    {
        var query = new FullTextSearchQuery(Guid.NewGuid(), null, null, null, null, null, null, null, null, null, 1, pageSize);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Validate_ValidPageSize_Passes(int pageSize)
    {
        var query = new FullTextSearchQuery(Guid.NewGuid(), null, null, null, null, null, null, null, null, null, 1, pageSize);
        var result = await _validator.ValidateAsync(query);
        result.IsValid.Should().BeTrue();
    }
}
