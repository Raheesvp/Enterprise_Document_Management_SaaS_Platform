using DocumentService.Application.Queries.GetDocumentList;
using DocumentService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace DocumentService.UnitTests.Application.Queries;

public class GetDocumentListQueryHandlerTests
{
    private readonly Mock<IDocumentReadRepository> _readRepo = new();

    private GetDocumentListQueryHandler CreateHandler()
        => new(_readRepo.Object);

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPaginatedList()
    {
        var tenantId = Guid.NewGuid();

        var summaries = new List<DocumentSummary>
        {
            new(Guid.NewGuid(), tenantId, "Doc 1.pdf",
                "Active", "Pdf", "application/pdf",
                1024 * 1024, 1, Guid.NewGuid(), "Test User",
                DateTime.UtcNow, DateTime.UtcNow, null, null),

            new(Guid.NewGuid(), tenantId, "Doc 2.docx",
                "Uploading", "Word", "application/msword",
                2 * 1024 * 1024, 1, Guid.NewGuid(), "Test User",
                DateTime.UtcNow, DateTime.UtcNow, null, "draft"),
        };

        var pagedResult = new PagedResult<DocumentSummary>(
            summaries.AsReadOnly(), TotalCount: 2, Page: 1, PageSize: 20);

        _readRepo
            .Setup(r => r.GetPagedAsync(tenantId,
                It.IsAny<DocumentQueryFilter>(), default))
            .ReturnsAsync(pagedResult);

        var result = await CreateHandler()
            .Handle(new GetDocumentListQuery(tenantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_EmptyTenant_ReturnsEmptyList()
    {
        var tenantId = Guid.NewGuid();

        var emptyResult = new PagedResult<DocumentSummary>(
            new List<DocumentSummary>().AsReadOnly(),
            TotalCount: 0, Page: 1, PageSize: 20);

        _readRepo
            .Setup(r => r.GetPagedAsync(tenantId,
                It.IsAny<DocumentQueryFilter>(), default))
            .ReturnsAsync(emptyResult);

        var result = await CreateHandler()
            .Handle(new GetDocumentListQuery(tenantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_SecondPage_ReturnsCorrectPaginationFlags()
    {
        var tenantId = Guid.NewGuid();

        var summaries = Enumerable.Range(1, 5)
            .Select(i => new DocumentSummary(
                Guid.NewGuid(), tenantId, $"Doc {i}.pdf",
                "Active", "Pdf", "application/pdf",
                1024, 1, Guid.NewGuid(), "Test User",
                DateTime.UtcNow, DateTime.UtcNow, null, null))
            .ToList().AsReadOnly();

        var pagedResult = new PagedResult<DocumentSummary>(
            summaries, TotalCount: 25, Page: 2, PageSize: 20);

        _readRepo
            .Setup(r => r.GetPagedAsync(tenantId,
                It.IsAny<DocumentQueryFilter>(), default))
            .ReturnsAsync(pagedResult);

        var result = await CreateHandler()
            .Handle(new GetDocumentListQuery(tenantId, Page: 2, PageSize: 20), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeTrue();
    }
}
