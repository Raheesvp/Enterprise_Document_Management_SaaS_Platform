using DocumentService.Application.DTOs;
using DocumentService.Application.Interfaces;
using DocumentService.Domain.Errors;
using DocumentService.Domain.Repositories;
using DocumentService.Domain.ValueObjects;
using MediatR;
using Shared.Domain.Common;

namespace DocumentService.Application.Commands.AddDocumentVersion;

public sealed class AddDocumentVersionCommandHandler
    : IRequestHandler<AddDocumentVersionCommand, Result<DocumentVersionDto>>
{
    private readonly IDocumentRepository _documentRepo;
    private readonly IStorageService _storageService;

    public AddDocumentVersionCommandHandler(
        IDocumentRepository documentRepo,
        IStorageService storageService)
    {
        _documentRepo   = documentRepo;
        _storageService = storageService;
    }

    public async Task<Result<DocumentVersionDto>> Handle(
        AddDocumentVersionCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Load full aggregate — needed because we are writing
        //    Commands always use IDocumentRepository (EF Core)
        //    Never IDocumentReadRepository (Dapper) for writes
        var document = await _documentRepo.GetByIdAsync(
            command.DocumentId,
            command.TenantId,
            cancellationToken);

        if (document is null)
            return Result.Failure<DocumentVersionDto>(
                DocumentErrors.Document.NotFound(command.DocumentId));

        // 2. Build Value Objects — validates business rules
        FileSize fileSize;
        try
        {
            fileSize = FileSize.FromBytes(command.FileSizeBytes);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<DocumentVersionDto>(
                new Error("Version.ValidationFailed", ex.Message));
        }

        // 3. Generate version-specific storage path
        //    Format: {tenantId}/{year}/{month}/{documentId}/v{N}_{title}
        var versionNumber = document.Versions.Count + 1;
        var fileName      = $"v{versionNumber}_{document.Title.Value}";
        var storagePath   = StoragePath.Create(
            command.TenantId,
            command.DocumentId,
            fileName);

        // 4. Upload new version file to MinIO
        //    If upload fails — exception thrown — no DB change made
        //    This prevents orphaned version records with no file
        await _storageService.UploadAsync(
            storagePath.Value,
            command.FileContent,
            command.MimeType,
            cancellationToken);

        // 5. Add version to aggregate
        //    Document.AddVersion() enforces:
        //    → Document must be Active status
        //    → Old versions marked IsCurrentVersion = false
        //    → New version marked IsCurrentVersion = true
        //    → DocumentVersionAddedEvent raised
        try
        {
            document.AddVersion(
                storagePath,
                fileSize,
                command.UploadedByUserId);
        }
        catch (InvalidOperationException ex)
        {
            // Clean up uploaded file — version record not saved
            await _storageService.DeleteAsync(
                storagePath.Value,
                cancellationToken);

            return Result.Failure<DocumentVersionDto>(
                new Error("Document.VersionUploadNotAllowed", ex.Message));
        }

        // 6. Persist updated aggregate
        await _documentRepo.UpdateAsync(document, cancellationToken);

        // 7. Return new version details
        var newVersion = document.CurrentVersion!;
        return Result.Success(new DocumentVersionDto(
            newVersion.Id,
            newVersion.VersionNumber,
            newVersion.FileSize.Bytes,
            newVersion.FileSize.ToString(),
            newVersion.StoragePath.Value,
            newVersion.IsCurrentVersion,
            newVersion.UploadedByUserId,
            newVersion.CreatedAt,
            newVersion.ExtractedText,
            newVersion.PageCount));
    }
}