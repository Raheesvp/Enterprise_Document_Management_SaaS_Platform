using MediatR;
using Shared.Domain.Common;
using DocumentService.Application.DTOs;

namespace DocumentService.Application.Commands.UploadDocument;

public record UploadDocumentCommand(
    Guid    TenantId,
    Guid    UploadedByUserId,
    string  UploadedByName,
    string  Title,
    string  MimeType,
    long    FileSizeBytes,
    Stream  FileContent,
    string? Description = null,
    string? Tags        = null,
    Guid?   DocumentId  = null) : IRequest<Result<DocumentDto>>;
