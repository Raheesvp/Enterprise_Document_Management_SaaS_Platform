using MediatR;
using Shared.Domain.Common;
using DocumentService.Application.DTOs;

namespace DocumentService.Application.Queries.GetDocumentVersions;

// GetDocumentVersionsQuery — full version history of a document
//
// Used in the document detail page
// Shows every version ever uploaded with file size + uploader
// If OCR is complete — shows extracted text preview per version
public record GetDocumentVersionsQuery(
    Guid DocumentId,
    Guid TenantId) : IRequest<Result<IReadOnlyList<DocumentVersionDto>>>;