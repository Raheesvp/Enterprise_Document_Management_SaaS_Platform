using MediatR;
using Shared.Domain.Common;
using DocumentService.Application.DTOs;

namespace DocumentService.Application.Queries.GetDocument;

// GetDocumentQuery — fetch single document by ID
//
// TenantId is ALWAYS required alongside DocumentId
// This prevents Tenant A from reading Tenant B's documents
// Even if they guess the DocumentId (a Guid)
// The repository filters by BOTH — no match = not found
public record GetDocumentQuery(
    Guid DocumentId,
    Guid TenantId) : IRequest<Result<DocumentDto>>;