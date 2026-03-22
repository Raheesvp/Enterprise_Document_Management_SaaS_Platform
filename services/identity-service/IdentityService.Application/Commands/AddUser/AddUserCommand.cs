using IdentityService.Application.DTOs;
using MediatR;
using Shared.Domain.Common;

namespace IdentityService.Application.Commands.AddUser;

// AddUserCommand — Admin only
// Allows Admin to invite Manager or Viewer to their tenant
public record AddUserCommand(
    Guid   TenantId,
    string FullName,
    string Email,
    string Password,
    string Role) : IRequest<Result<UserCreatedDto>>;

public record UserCreatedDto(
    Guid   UserId,
    string Email,
    string Role);
