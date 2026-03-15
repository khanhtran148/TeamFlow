using TeamFlow.Application.Common.Models;

namespace TeamFlow.Tests.Application;

public class PagedResultTests
{
    [Fact]
    public void PagedResult_ShouldCalculateTotalPagesCorrectly()
    {
        var result = new PagedResult<string>([], 100, 1, 20);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public void PagedResult_HasNextPage_WhenNotLastPage()
    {
        var result = new PagedResult<string>([], 100, 1, 20);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_NoNextPage_WhenLastPage()
    {
        var result = new PagedResult<string>([], 100, 5, 20);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void PagedResult_HasNoPreviousPage_WhenFirstPage()
    {
        var result = new PagedResult<string>([], 100, 1, 20);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void PagedResult_HasPreviousPage_WhenNotFirstPage()
    {
        var result = new PagedResult<string>([], 100, 2, 20);
        Assert.True(result.HasPreviousPage);
    }

    [Fact]
    public void PagedResult_SinglePage_BothFlagsAreFalse()
    {
        var result = new PagedResult<string>([], 10, 1, 20);
        Assert.False(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Equal(1, result.TotalPages);
    }
}
