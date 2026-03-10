using MediatR;
using Shared.Domain.Common;
using DocumentService.Application.DTOs;

namespace DocumentService.Application.Commands.AddDocumentVersion;

// AddDocumentVersionCommand — uploads a new version to existing document
//
// When to use this vs UploadDocumentCommand:
// UploadDocumentCommand    → brand new document (first time)
// AddDocumentVersionCommand → updated file on existing document
//
// Real world example:
// User uploads "Contract.pdf" → UploadDocumentCommand (creates Doc + V1)
// Legal edits it and re-uploads → AddDocumentVersionCommand (creates V2)
// Old version V1 is preserved — never deleted
// CurrentVersion pointer moves to V2
public record AddDocumentVersionCommand(
    Guid DocumentId,
    Guid TenantId,
    Guid UploadedByUserId,
    long FileSizeBytes,
    Stream FileContent,
    string MimeType) : IRequest<Result<DocumentVersionDto>>;