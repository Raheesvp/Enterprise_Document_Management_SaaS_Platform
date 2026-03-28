using DocumentService.Application.Interfaces;
using DocumentService.Application.Queries.GetDocumentList;
using DocumentService.Domain.Enums;
using DocumentService.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocumentService.UnitTests.Application.Queries;

public class GetDocumentListQueryHandlerTests
{
    private readonly Mock<IDocumentReadRepository> _readRepo = new();
    private readonly Mock<IStorageService> _storageService = new();

    private GetDocumentListQueryHandler CreateHandler()
        => new(_readRepo.Object, _storageService.Object);

    [Fact]
    public async Task Handle_ReturnsPagedDocuments_WithDownloadUrls()
    {
        var tenantId = Guid.NewGuid();
        var uploadedByUserId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var paged = new PagedResult<DocumentSummary>(
            new[]
            {
                new DocumentSummary(
                    Guid.NewGuid(),
                    tenantId,
                    "Doc1",
                    "Active",
                    "Pdf",
                    "application/pdf",
                    1024,
                    "documents/tenant/doc1.pdf",
                    1,
                    uploadedByUserId,
                    "User A",
                    createdAt,
                    createdAt,
                    "Desc 1",
                    "tag1")
            },
            1,
            1,
            10);

        _readRepo
            .Setup(r => r.GetPagedAsync(
                tenantId,
                It.IsAny<DocumentQueryFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        _storageService
            .Setup(s => s.GetPresignedUrlAsync(
                "documents/tenant/doc1.pdf",
                "application/pdf",
                30))
            .ReturnsAsync("https://signed.example/doc1.pdf");

        var result = await CreateHandler().Handle(
            new GetDocumentListQuery(tenantId),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Title.Should().Be("Doc1");
        result.Value.Items[0].FileSizeFormatted.Should().Be("1.00 KB");
        result.Value.Items[0].UploadedByName.Should().Be("User A");
        result.Value.Items[0].DownloadUrl.Should().Be(
            "https://signed.example/doc1.pdf");
    }

    [Fact]
    public async Task Handle_PassesQueryFiltersToRepository()
    {
        var tenantId = Guid.NewGuid();
        DocumentQueryFilter? capturedFilter = null;

        _readRepo
            .Setup(r => r.GetPagedAsync(
                tenantId,
                It.IsAny<DocumentQueryFilter>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, DocumentQueryFilter, CancellationToken>(
                (_, filter, _) => capturedFilter = filter)
            .ReturnsAsync(new PagedResult<DocumentSummary>(
                Array.Empty<DocumentSummary>(),
                0,
                2,
                25));

        var query = new GetDocumentListQuery(
            tenantId,
            Status: DocumentStatus.Active,
            Type: DocumentType.Pdf,
            SearchTerm: "invoice",
            FromDate: new DateTime(2026, 1, 1),
            ToDate: new DateTime(2026, 1, 31),
            Page: 2,
            PageSize: 25);

        var result = await CreateHandler().Handle(query, default);

        result.IsSuccess.Should().BeTrue();
        capturedFilter.Should().NotBeNull();
        capturedFilter!.Status.Should().Be(DocumentStatus.Active);
        capturedFilter.Type.Should().Be(DocumentType.Pdf);
        capturedFilter.SearchTerm.Should().Be("invoice");
        capturedFilter.FromDate.Should().Be(new DateTime(2026, 1, 1));
        capturedFilter.ToDate.Should().Be(new DateTime(2026, 1, 31));
        capturedFilter.Page.Should().Be(2);
        capturedFilter.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Handle_NoDocuments_ReturnsEmptyList_AndSkipsSignedUrls()
    {
        var tenantId = Guid.NewGuid();

        _readRepo
            .Setup(r => r.GetPagedAsync(
                tenantId,
                It.IsAny<DocumentQueryFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<DocumentSummary>(
                Array.Empty<DocumentSummary>(),
                0,
                1,
                10));

        var result = await CreateHandler().Handle(
            new GetDocumentListQuery(tenantId),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.HasNextPage.Should().BeFalse();
        result.Value.HasPreviousPage.Should().BeFalse();

        _storageService.Verify(s => s.GetPresignedUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<int>()),
            Times.Never);
    }
}
