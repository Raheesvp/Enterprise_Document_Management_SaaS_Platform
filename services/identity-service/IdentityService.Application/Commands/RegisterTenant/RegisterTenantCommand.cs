using MediatR;
using Shared.Domain.Common;
using IdentityService.Application.DTOs;

namespace IdentityService.Application.Commands.RegisterTenant;

public record RegisterTenantCommand(
    string TenantName,
    string Subdomain,
    string ContactEmail,
    string AdminFullName,
    string AdminEmail,
    string AdminPassword) : IRequest<Result<TenantDto>>;