using DocumentService.Application.Queries.GetDocument;
using DocumentService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace DocumentService.UnitTests.Application.Queries;

public class GetDocumentQueryHandlerTests
{
    private readonly Mock<IDocumentReadRepository> _readRepo = new();

    private GetDocumentQueryHandler CreateHandler()
        => new(_readRepo.Object);

    [Fact]
    public async Task Handle_ExistingDocument_ReturnsSuccessWithDto()
    {
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        var summary = new DocumentSummary(
            documentId, tenantId, "Invoice Q1.pdf",
            "Active", "Pdf", "application/pdf",
            1024 * 1024, 1,
            Guid.NewGuid(), "Test User",
            DateTime.UtcNow.AddDays(-1), DateTime.UtcNow,
            "Finance", "invoice,q1");

        _readRepo
            .Setup(r => r.GetSummaryByIdAsync(documentId, tenantId, default))
            .ReturnsAsync(summary);

        var result = await CreateHandler()
            .Handle(new GetDocumentQuery(documentId, tenantId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(documentId);
        result.Value.Title.Should().Be("Invoice Q1.pdf");
        result.Value.Status.Should().Be("Active");
        result.Value.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsFailure()
    {
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _readRepo
            .Setup(r => r.GetSummaryByIdAsync(documentId, tenantId, default))
            .ReturnsAsync((DocumentSummary?)null);

        var result = await CreateHandler()
            .Handle(new GetDocumentQuery(documentId, tenantId), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");
    }

    [Fact]
    public async Task Handle_DocumentFromDifferentTenant_ReturnsFailure()
    {
        var wrongTenantId = Guid.NewGuid();
        var documentId    = Guid.NewGuid();

        _readRepo
            .Setup(r => r.GetSummaryByIdAsync(documentId, wrongTenantId, default))
            .ReturnsAsync((DocumentSummary?)null);

        var result = await CreateHandler()
            .Handle(new GetDocumentQuery(documentId, wrongTenantId), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");
    }
}
