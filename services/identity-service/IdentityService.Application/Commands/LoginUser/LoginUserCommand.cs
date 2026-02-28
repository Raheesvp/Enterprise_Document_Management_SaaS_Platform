using IdentityService.Application.DTOs;
using MediatR;
using Shared.Domain.Common;

namespace IdentityService.Application.Commands.LoginUser;

public record LoginUserCommand(
    string Email,
    string Password,
    string Subdomain) : IRequest<Result<AuthResponseDto>>;