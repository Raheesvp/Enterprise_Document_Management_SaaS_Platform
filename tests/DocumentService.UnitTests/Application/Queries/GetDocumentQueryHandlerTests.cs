using DocumentService.Application.Interfaces;
using DocumentService.Application.Queries.GetDocument;
using DocumentService.Domain.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace DocumentService.UnitTests.Application.Queries;

public class GetDocumentQueryHandlerTests
{
    private readonly Mock<IDocumentReadRepository> _readRepo = new();
    private readonly Mock<IStorageService> _storageService = new();

    private GetDocumentQueryHandler CreateHandler()
        => new(_readRepo.Object, _storageService.Object);

    [Fact]
    public async Task Handle_ExistingDocument_ReturnsSuccessWithDto()
    {
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var uploadedByUserId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        var summary = new DocumentSummary(
            documentId,
            tenantId,
            "Invoice.pdf",
            "Active",
            "Pdf",
            "application/pdf",
            2048,
            "documents/tenant/invoice.pdf",
            1,
            uploadedByUserId,
            "Rahees",
            createdAt,
            updatedAt,
            "Tax Invoice",
            "finance");

        _readRepo
            .Setup(r => r.GetSummaryByIdAsync(
                documentId,
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        _storageService
            .Setup(s => s.GetPresignedUrlAsync(
                "documents/tenant/invoice.pdf",
                "application/pdf",
                30))
            .ReturnsAsync("https://signed.example/invoice.pdf");

        var result = await CreateHandler().Handle(
            new GetDocumentQuery(documentId, tenantId),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(documentId);
        result.Value.Title.Should().Be("Invoice.pdf");
        result.Value.StoragePath.Should().Be("documents/tenant/invoice.pdf");
        result.Value.FileSizeFormatted.Should().Be("2.00 KB");
        result.Value.UploadedByUserId.Should().Be(uploadedByUserId.ToString());
        result.Value.Description.Should().Be("Tax Invoice");
        result.Value.Tags.Should().Be("finance");
        result.Value.DownloadUrl.Should().Be(
            "https://signed.example/invoice.pdf");
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _readRepo
            .Setup(r => r.GetSummaryByIdAsync(
                documentId,
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentSummary?)null);

        var result = await CreateHandler().Handle(
            new GetDocumentQuery(documentId, tenantId),
            default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");

        _storageService.Verify(s => s.GetPresignedUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UsesStoragePathAndMimeType_ForSignedUrl()
    {
        var tenantId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var summary = new DocumentSummary(
            documentId,
            tenantId,
            "Contract.pdf",
            "Active",
            "Pdf",
            "application/pdf",
            512,
            "documents/tenant/contract.pdf",
            3,
            Guid.NewGuid(),
            "User B",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow,
            null,
            null);

        _readRepo
            .Setup(r => r.GetSummaryByIdAsync(
                documentId,
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        _storageService
            .Setup(s => s.GetPresignedUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<int>()))
            .ReturnsAsync("signed-url");

        var result = await CreateHandler().Handle(
            new GetDocumentQuery(documentId, tenantId),
            default);

        result.IsSuccess.Should().BeTrue();

        _storageService.Verify(s => s.GetPresignedUrlAsync(
            "documents/tenant/contract.pdf",
            "application/pdf",
            30),
            Times.Once);
    }
}
