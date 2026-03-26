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
    private readonly IStorageService     _storageService;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepo,
        IStorageService     storageService)
    {
        _documentRepo   = documentRepo;
        _storageService = storageService;
    }

    public async Task<Result<DocumentDto>> Handle(
        UploadDocumentCommand command,
        CancellationToken cancellationToken)
    {
        DocumentTitle title;
        FileSize      fileSize;
        ContentType   contentType;

        try
        {
            title       = DocumentTitle.Create(command.Title);
            fileSize    = FileSize.FromBytes(command.FileSizeBytes);
            contentType = ContentType.Create(command.MimeType);

            if (contentType.DocumentType ==
                Domain.Enums.DocumentType.Other)
            {
                return Result.Failure<DocumentDto>(
                    new Error("Document.UnsupportedType",
                        $"File type '{command.MimeType}' not allowed."));
            }
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<DocumentDto>(
                new Error("Document.ValidationFailed", ex.Message));
        }

        var documentId  = Guid.NewGuid();
        var fileName    = SanitizeFileName(command.Title);
        var storagePath = StoragePath.Create(
            command.TenantId, documentId, fileName);

        await _storageService.UploadAsync(
            storagePath.Value,
            command.FileContent,
            command.MimeType,
            cancellationToken);

        Document document;
        if (command.DocumentId.HasValue)
        {
            var existing = await _documentRepo.GetByIdAsync(
                command.DocumentId.Value, command.TenantId, cancellationToken);
            
            if (existing is null || existing.TenantId != command.TenantId)
            {
                return Result.Failure<DocumentDto>(
                    new Error("Document.NotFound", 
                        $"Document with ID {command.DocumentId} not found."));
            }

            existing.AddVersion(storagePath, fileSize, command.UploadedByUserId);
            document = existing;
            await _documentRepo.UpdateAsync(document, cancellationToken);
        }
        else
        {
            document = Document.Create(
                command.TenantId,
                command.UploadedByUserId,
                command.UploadedByName,
                title,
                contentType,
                storagePath,
                fileSize);

            if (command.Description is not null)
                document.UpdateDescription(command.Description);

            if (command.Tags is not null)
                document.UpdateTags(command.Tags);

            document.MarkAsProcessing();
            document.MarkAsActive();

            await _documentRepo.AddAsync(document, cancellationToken);
        }

        return Result.Success(ToDto(document));
    }

    private static string SanitizeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(
            title.Select(c => invalid.Contains(c) ? '_' : c));
    }

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
