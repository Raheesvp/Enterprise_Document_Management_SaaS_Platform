using DocumentService.Application.Commands.UploadDocument;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;
using FluentAssertions;
using MassTransit;
using Moq;
using Xunit;

namespace DocumentService.UnitTests.Application.Commands;

public class UploadDocumentCommandHandlerTests
{
    private readonly Mock<IDocumentRepository> _repo = new();
    private readonly Mock<IStorageService> _storage = new();
    // 1. Added missing semicolon and fixed the mock declaration
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();

    // 2. Updated to include the third parameter required by the constructor
    private UploadDocumentCommandHandler CreateHandler()
        => new(_repo.Object, _storage.Object, _publishEndpointMock.Object);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithDocumentDto()
    {
        // Arrange
        _storage
            .Setup(s => s.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("tenant/2025/01/doc/file.pdf");

        _repo
            .Setup(r => r.AddAsync(
                It.IsAny<Document>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new UploadDocumentCommand(
            TenantId: Guid.NewGuid(), 
            UploadedByUserId: Guid.NewGuid(), 
            UploadedByName: "Test User", 
            Title: "Invoice Q1.pdf", 
            MimeType: "application/pdf", 
            FileSizeBytes: 1024 * 1024, 
            FileContent: new MemoryStream(new byte[100]));

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Invoice Q1.pdf");
        result.Value.Status.Should().Be("Active");
        result.Value.VersionCount.Should().Be(1);

        // Verify storage and repo called
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // 3. Optional: Verify that the integration event was actually published
        _publishEndpointMock.Verify(p => p.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FileSizeExceeds500MB_ReturnsFailure()
    {
        // Arrange — 600MB exceeds the 500MB domain rule
        var command = new UploadDocumentCommand(
            TenantId: Guid.NewGuid(), 
            UploadedByUserId: Guid.NewGuid(), 
            UploadedByName: "Test User", 
            Title: "HugeFile.pdf", 
            MimeType: "application/pdf", 
            FileSizeBytes: 600L * 1024 * 1024, 
            FileContent: new MemoryStream());

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.Verify(r => r.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ReturnsFailure()
    {
        var command = new UploadDocumentCommand(
            TenantId: Guid.NewGuid(), 
            UploadedByUserId: Guid.NewGuid(), 
            UploadedByName: "Test User", 
            Title: "", 
            MimeType: "application/pdf", 
            FileSizeBytes: 1024, 
            FileContent: new MemoryStream());

        var result = await CreateHandler().Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnsupportedMimeType_ReturnsFailure()
    {
        var command = new UploadDocumentCommand(
            TenantId: Guid.NewGuid(), 
            UploadedByUserId: Guid.NewGuid(), 
            UploadedByName: "Test User", 
            Title: "Virus.exe", 
            MimeType: "application/exe", 
            FileSizeBytes: 1024, 
            FileContent: new MemoryStream());

        var result = await CreateHandler().Handle(command, default);

        result.IsFailure.Should().BeTrue();
        _storage.Verify(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateTitle_AddsNewVersion()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var title    = "Existing Document";
        
        var existingDoc = Document.Create(
            tenantId, userId, "Test User",
            DocumentService.Domain.ValueObjects.DocumentTitle.Create(title),
            DocumentService.Domain.ValueObjects.ContentType.Create("application/pdf"),
            DocumentService.Domain.ValueObjects.StoragePath.Create(tenantId, Guid.NewGuid(), "v1_existing.pdf"),
            DocumentService.Domain.ValueObjects.FileSize.FromBytes(1024));

        existingDoc.MarkAsProcessing();
        existingDoc.MarkAsActive();

        _repo
            .Setup(r => r.GetByTitleAsync(title, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDoc);

        _repo
            .Setup(r => r.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _storage
            .Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path/to/new/version.pdf");

        var command = new UploadDocumentCommand(
            TenantId: tenantId,
            UploadedByUserId: userId,
            UploadedByName: "Test User",
            Title: title,
            MimeType: "application/pdf",
            FileSizeBytes: 2048,
            FileContent: new MemoryStream(new byte[100]));

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VersionCount.Should().Be(2);
        result.Value.CurrentVersionNumber.Should().Be(2);

        _repo.Verify(r => r.GetByTitleAsync(title, tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.UpdateAsync(existingDoc, It.IsAny<CancellationToken>()), Times.Once);
    }
}