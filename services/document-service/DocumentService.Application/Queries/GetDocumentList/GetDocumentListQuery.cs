using MediatR;
using Shared.Domain.Common;
using DocumentService.Application.DTOs;
using DocumentService.Domain.Enums;

namespace DocumentService.Application.Queries.GetDocumentList;

// GetDocumentListQuery — paginated list with filters
//
// All filters are optional — if not provided, returns all documents
// for the tenant ordered by CreatedAt descending
//
// Pagination is enforced — PageSize max 100
// No unbounded queries — protects against large data dumps
public record GetDocumentListQuery(
    Guid TenantId,
    DocumentStatus? Status    = null,
    DocumentType?   Type      = null,
    string?         SearchTerm = null,
    DateTime?       FromDate  = null,
    DateTime?       ToDate    = null,
    int             Page      = 1,
    int             PageSize  = 20) : IRequest<Result<DocumentListDto>>;