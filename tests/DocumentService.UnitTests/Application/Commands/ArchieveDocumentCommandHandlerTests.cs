using DocumentService.Application.Commands.ArchiveDocument;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Enums;
using DocumentService.Domain.Repositories;
using DocumentService.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace DocumentService.UnitTests.Application.Commands;

public class ArchiveDocumentCommandHandlerTests
{
    private readonly Mock<IDocumentRepository> _repo = new();

    private ArchiveDocumentCommandHandler CreateHandler()
        => new(_repo.Object);

    // Helper — creates a real Document aggregate for testing
    private static Document CreateActiveDocument(
        Guid tenantId, Guid documentId)
    {
        var doc = Document.Create(
            tenantId,
            Guid.NewGuid(),
            DocumentTitle.Create("Contract.pdf"),
            ContentType.Create("application/pdf"),
            StoragePath.Create(tenantId, documentId, "contract.pdf"),
            FileSize.FromBytes(1024 * 1024));

        // Move to Active status so Archive is valid
        doc.MarkAsProcessing();
        doc.MarkAsActive();
        return doc;
    }

    [Fact]
    public async Task Handle_ActiveDocument_ArchivesSuccessfully()
    {
        // Arrange
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var document   = CreateActiveDocument(tenantId, documentId);

        _repo
            .Setup(r => r.GetByIdAsync(documentId, tenantId, default))
            .ReturnsAsync(document);

        _repo
            .Setup(r => r.UpdateAsync(
                It.IsAny<Document>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ArchiveDocumentCommand(
            documentId, tenantId, Guid.NewGuid());

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        document.Status.Should().Be(DocumentStatus.Archived);

        _repo.Verify(r => r.UpdateAsync(
            It.IsAny<Document>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsFailure()
    {
        // Arrange — repository returns null
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _repo
            .Setup(r => r.GetByIdAsync(documentId, tenantId, default))
            .ReturnsAsync((Document?)null);

        var command = new ArchiveDocumentCommand(
            documentId, tenantId, Guid.NewGuid());

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");

        // Update must never be called if document not found
        _repo.Verify(r => r.UpdateAsync(
            It.IsAny<Document>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyArchivedDocument_ReturnsFailure()
    {
        // Arrange — document already archived
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var document   = CreateActiveDocument(tenantId, documentId);
        document.Archive(); // Archive it first

        _repo
            .Setup(r => r.GetByIdAsync(documentId, tenantId, default))
            .ReturnsAsync(document);

        var command = new ArchiveDocumentCommand(
            documentId, tenantId, Guid.NewGuid());

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert — double archive must fail
        result.IsFailure.Should().BeTrue();
    }
}