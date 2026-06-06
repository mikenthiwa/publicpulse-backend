using FluentAssertions;
using Web.Common.Models;

namespace Web.UnitTests.Infrastructure;

public sealed class PaginatedListTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(20, 10, 2)]
    [InlineData(21, 10, 3)]
    public void TotalPages_ShouldRoundUp(int count, int pageSize, int expectedTotalPages)
    {
        var paginatedList = CreatePaginatedList(count, pageNumber: 1, pageSize);

        paginatedList.TotalPages.Should().Be(expectedTotalPages);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void HasPreviousPage_ShouldReflectPageNumber(int pageNumber, bool expected)
    {
        var paginatedList = CreatePaginatedList(count: 20, pageNumber, pageSize: 10);

        paginatedList.HasPreviousPage.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 25, 10)]
    [InlineData(2, 25, 10)]
    public void HasNextPage_ShouldBeTrue_WhenAnotherPageExists(int pageNumber, int count, int pageSize)
    {
        var paginatedList = CreatePaginatedList(count, pageNumber, pageSize);

        paginatedList.HasNextPage.Should().BeTrue();
    }

    [Theory]
    [InlineData(3, 25, 10)]
    [InlineData(2, 20, 10)]
    [InlineData(1, 5, 10)]
    [InlineData(1, 0, 10)]
    public void HasNextPage_ShouldBeFalse_WhenOnOrBeyondFinalPage(int pageNumber, int count, int pageSize)
    {
        var paginatedList = CreatePaginatedList(count, pageNumber, pageSize);

        paginatedList.HasNextPage.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    public void Constructor_WithInvalidPageNumber_ShouldThrow(int pageNumber, int pageSize)
    {
        var action = () => CreatePaginatedList(count: 0, pageNumber, pageSize);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Constructor_WithInvalidPageSize_ShouldThrow(int pageNumber, int pageSize)
    {
        var action = () => CreatePaginatedList(count: 0, pageNumber, pageSize);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static PaginatedList<object> CreatePaginatedList(int count, int pageNumber, int pageSize)
    {
        return new PaginatedList<object>([], count, pageNumber, pageSize);
    }
}
