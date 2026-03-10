using DocumentService.Application.Commands.UploadDocument;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace DocumentService.UnitTests.Application.Commands;

public class UploadDocumentCommandHandlerTests
{
    private readonly Mock<IDocumentRepository> _repo    = new();
    private readonly Mock<IStorageService>     _storage = new();

    private UploadDocumentCommandHandler CreateHandler()
        => new(_repo.Object, _storage.Object);

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
            TenantId:        Guid.NewGuid(),
            UploadedByUserId: Guid.NewGuid(),
            Title:           "Invoice Q1.pdf",
            MimeType:        "application/pdf",
            FileSizeBytes:   1024 * 1024,
            FileContent:     new MemoryStream(new byte[100]));

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Invoice Q1.pdf");
        result.Value.Status.Should().Be("Uploading");
        result.Value.VersionCount.Should().Be(1);

        // Verify storage was called exactly once
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify repository was called exactly once
        _repo.Verify(r => r.AddAsync(
            It.IsAny<Document>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FileSizeExceeds500MB_ReturnsFailure()
    {
        // Arrange — 600MB exceeds the 500MB domain rule
        var command = new UploadDocumentCommand(
            TenantId:        Guid.NewGuid(),
            UploadedByUserId: Guid.NewGuid(),
            Title:           "HugeFile.pdf",
            MimeType:        "application/pdf",
            FileSizeBytes:   600L * 1024 * 1024,
            FileContent:     new MemoryStream());

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();

        // Storage must NEVER be called for oversized files
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);

        // Repository must NEVER be called either
        _repo.Verify(r => r.AddAsync(
            It.IsAny<Document>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyTitle_ReturnsFailure()
    {
        var command = new UploadDocumentCommand(
            TenantId:        Guid.NewGuid(),
            UploadedByUserId: Guid.NewGuid(),
            Title:           "",
            MimeType:        "application/pdf",
            FileSizeBytes:   1024,
            FileContent:     new MemoryStream());

        var result = await CreateHandler().Handle(command, default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnsupportedMimeType_ReturnsFailure()
    {
        var command = new UploadDocumentCommand(
            TenantId:        Guid.NewGuid(),
            UploadedByUserId: Guid.NewGuid(),
            Title:           "Virus.exe",
            MimeType:        "application/exe",
            FileSizeBytes:   1024,
            FileContent:     new MemoryStream());

        var result = await CreateHandler().Handle(command, default);

        result.IsFailure.Should().BeTrue();

        // Storage never called for unsupported types
        _storage.Verify(s => s.UploadAsync(
            It.IsAny<string>(),
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}