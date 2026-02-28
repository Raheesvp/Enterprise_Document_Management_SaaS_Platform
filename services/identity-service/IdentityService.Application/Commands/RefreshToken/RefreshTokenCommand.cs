using IdentityService.Application.DTOs;
using MediatR;
using Shared.Domain.Common;

namespace IdentityService.Application.Commands.RefreshToken;

public record RefreshTokenCommand(
    Guid UserId,
    string Token) : IRequest<Result<AuthResponseDto>>;