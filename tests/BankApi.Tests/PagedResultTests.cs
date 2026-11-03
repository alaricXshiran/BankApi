using BankApi.Dtos;

namespace BankApi.Tests;

// Tests for PagedResult, especially the TotalPages calculation.
public class PagedResultTests
{
    // [Fact] marks a single test with fixed values.
    [Fact]
    public void TotalPages_RoundsUp_WhenLastPageIsPartlyFull()
    {
        // Arrange: 47 accounts, 10 per page.
        var result = new PagedResult<string>
        {
            TotalCount = 47,
            PageSize = 10
        };

        // Act: read the calculated number of pages.
        var totalPages = result.TotalPages;

        // Assert: 47 accounts need 5 pages (the last one has only 7).
        Assert.Equal(5, totalPages);
    }

    // [Theory] runs the same test several times, once for each [InlineData] row.
    [Theory]
    [InlineData(0, 10, 0)]   // no accounts at all: no pages
    [InlineData(10, 10, 1)]  // exactly one full page
    [InlineData(11, 10, 2)]  // one account spills onto a second page
    [InlineData(100, 25, 4)] // divides evenly
    public void TotalPages_IsCalculatedCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Arrange
        var result = new PagedResult<string>
        {
            TotalCount = totalCount,
            PageSize = pageSize
        };

        // Act and Assert
        Assert.Equal(expectedPages, result.TotalPages);
    }
}