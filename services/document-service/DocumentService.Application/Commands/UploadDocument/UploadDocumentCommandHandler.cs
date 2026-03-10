using DocumentService.Application.DTOs;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Entities;
using DocumentService.Domain.Repositories;
using DocumentService.Domain.ValueObjects;
using MediatR;
using Shared.Domain.Common;

namespace DocumentService.Application.Commands.UploadDocument;

public sealed class UploadDocumentCommandHandler
    : IRequestHandler<UploadDocumentCommand, Result<DocumentDto>>
{
    private readonly IDocumentRepository _documentRepo;
    private readonly IStorageService _storageService;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepo,
        IStorageService storageService)
    {
        _documentRepo = documentRepo;
        _storageService = storageService;
    }

    public async Task<Result<DocumentDto>> Handle(
        UploadDocumentCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Build Value Objects — enforces domain business rules
        DocumentTitle title;
        FileSize fileSize;
        ContentType contentType;

        try
        {
            title       = DocumentTitle.Create(command.Title);
            fileSize    = FileSize.FromBytes(command.FileSizeBytes);
            contentType = ContentType.Create(command.MimeType);



            if (contentType.DocumentType == Domain.Enums.DocumentType.Other)
    {
        return Result.Failure<DocumentDto>(
            new Error("Document.UnsupportedType", $"The file type '{command.MimeType}' is not allowed."));
    }

        }

        

        catch (ArgumentException ex)
        {
            return Result.Failure<DocumentDto>(
                new Error("Document.ValidationFailed", ex.Message));
        }

        // 2. Generate storage path BEFORE creating aggregate
        // Path format: {tenantId}/{year}/{month}/{documentId}/{fileName}
        var documentId  = Guid.NewGuid();
        var fileName    = SanitizeFileName(command.Title);
        var storagePath = StoragePath.Create(
            command.TenantId,
            documentId,
            fileName);

        // 3. Upload file to MinIO/S3
        // If this fails — exception propagates, no DB record created
        // This prevents orphaned DB records with no file
        await _storageService.UploadAsync(
            storagePath.Value,
            command.FileContent,
            command.MimeType,
            cancellationToken);

        // 4. Create Document aggregate — raises DocumentCreatedEvent
        var document = Document.Create(
            command.TenantId,
            command.UploadedByUserId,
            title,
            contentType,
            storagePath,
            fileSize);

        if (command.Description is not null)
            document.UpdateDescription(command.Description);

        if (command.Tags is not null)
            document.UpdateTags(command.Tags);

        // 5. Persist to database
        // Domain events dispatched after SaveChangesAsync in infrastructure
        await _documentRepo.AddAsync(document, cancellationToken);

        return Result.Success(ToDto(document));
    }

    // Remove characters that are invalid in file paths
    private static string SanitizeFileName(string title)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(
            title.Select(c => invalidChars.Contains(c) ? '_' : c));
    }

    // Map Document aggregate → DocumentDto
    private static DocumentDto ToDto(Document doc)
    {
        var current = doc.CurrentVersion!;
        return new DocumentDto(
            doc.Id,
            doc.TenantId,
            doc.Title.Value,
            doc.Status.ToString(),
            doc.ContentType.DocumentType.ToString(),
            doc.ContentType.MimeType,
            current.FileSize.Bytes,
            current.FileSize.ToString(),
            doc.Versions.Count,
            current.VersionNumber,
            current.StoragePath.Value,
            doc.UploadedByUserId.ToString(),
            doc.CreatedAt,
            doc.UpdatedAt,
            doc.Description,
            doc.Tags);
    }
}