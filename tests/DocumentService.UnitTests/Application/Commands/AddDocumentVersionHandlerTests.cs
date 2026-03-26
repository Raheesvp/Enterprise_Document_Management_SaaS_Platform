using DocumentService.Application.Commands.AddDocumentVersion;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;
using DocumentService.Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace DocumentService.UnitTests.Application.Commands;

public class AddDocumentVersionCommandHandlerTests
{
    private readonly Mock<IDocumentRepository> _repo    = new();
    private readonly Mock<IStorageService>     _storage = new();

    private AddDocumentVersionCommandHandler CreateHandler()
        => new(_repo.Object, _storage.Object);

    private static Document CreateActiveDocument(
        Guid tenantId, Guid documentId)
    {
        var doc = Document.Create(tenantId, Guid.NewGuid(), "Test User", DocumentTitle.Create("Report.pdf"), ContentType.Create("application/pdf"), StoragePath.Create(tenantId, documentId, "report.pdf"),
            FileSize.FromBytes(1024 * 1024));

        doc.MarkAsProcessing();
        doc.MarkAsActive();
        return doc;
    }

    [Fact]
    public async Task Handle_ActiveDocument_AddsVersionSuccessfully()
    {
        // Arrange
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var document   = CreateActiveDocument(tenantId, documentId);

        _repo
            .Setup(r => r.GetByIdAsync(documentId, tenantId, default))
            .ReturnsAsync(document);

        _storage
            .Setup(s => s.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("path/to/v2/file.pdf");

        _repo
            .Setup(r => r.UpdateAsync(
                It.IsAny<Document>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new AddDocumentVersionCommand(
            documentId,
            tenantId,
            Guid.NewGuid(),
            2 * 1024 * 1024,
            new MemoryStream(new byte[100]),
            "application/pdf");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VersionNumber.Should().Be(2);
        result.Value.IsCurrentVersion.Should().BeTrue();

        // Storage called once for new version file
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsFailure()
    {
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _repo
            .Setup(r => r.GetByIdAsync(documentId, tenantId, default))
            .ReturnsAsync((Document?)null);

        var command = new AddDocumentVersionCommand(
            documentId, tenantId, Guid.NewGuid(),
            1024, new MemoryStream(), "application/pdf");

        var result = await CreateHandler().Handle(command, default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Document.NotFound");

        // Storage must never be called if document not found
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotActiveDocument_CleansUpFileAndReturnsFailure()
    {
        // Arrange — document still in Uploading status
        var tenantId   = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        // Create document but do NOT move to Active
        var document = Document.Create(tenantId, Guid.NewGuid(), "Test User", DocumentTitle.Create("Report.pdf"), ContentType.Create("application/pdf"), StoragePath.Create(tenantId, documentId, "report.pdf"),
            FileSize.FromBytes(1024));

        _repo
            .Setup(r => r.GetByIdAsync(documentId, tenantId, default))
            .ReturnsAsync(document);

        // Storage upload succeeds — but version add fails
        _storage
            .Setup(s => s.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("path/to/file.pdf");

        var command = new AddDocumentVersionCommand(
            documentId, tenantId, Guid.NewGuid(),
            1024, new MemoryStream(), "application/pdf");

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();

        // CRITICAL: Storage cleanup must be called
        // File was uploaded but version failed — must delete orphan
        _storage.Verify(s => s.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
