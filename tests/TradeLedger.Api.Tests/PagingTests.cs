using TradeLedger.Api.Common;
using Xunit;

namespace TradeLedger.Api.Tests;

public class PagingTests
{
    [Theory]
    [InlineData(1, 20, 1, 20)]
    [InlineData(0, 20, 1, 20)]
    [InlineData(-5, 20, 1, 20)]
    [InlineData(3, 0, 3, Paging.DefaultPageSize)]
    [InlineData(3, -10, 3, Paging.DefaultPageSize)]
    [InlineData(1, 1000, 1, Paging.MaxPageSize)]
    public void Normalize_clamps_page_and_pageSize_into_valid_ranges(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var (normalizedPage, normalizedPageSize) = Paging.Normalize(page, pageSize);

        Assert.Equal(expectedPage, normalizedPage);
        Assert.Equal(expectedPageSize, normalizedPageSize);
    }

    [Fact]
    public void Normalize_leaves_a_valid_page_and_pageSize_untouched()
    {
        var (page, pageSize) = Paging.Normalize(2, 50);

        Assert.Equal(2, page);
        Assert.Equal(50, pageSize);
    }
}
