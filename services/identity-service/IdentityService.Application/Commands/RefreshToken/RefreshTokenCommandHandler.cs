using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Errors;
using IdentityService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace IdentityService.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IJwtService _jwtService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepo,
        ITenantRepository tenantRepo,
        IJwtService jwtService)
    {
        _userRepo = userRepo;
        _tenantRepo = tenantRepo;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponseDto>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Find user
        var user = await _userRepo.GetByIdAsync(
            command.UserId, cancellationToken);

        if (user is null)
            return Result.Failure<AuthResponseDto>(IdentityErrors.Token.Invalid);

        // 2. Validate the refresh token
        var activeToken = user.GetActiveRefreshToken(command.Token);

        if (activeToken is null)
            return Result.Failure<AuthResponseDto>(IdentityErrors.Token.Invalid);

        // 3. Get tenant for JWT generation
        var tenant = await _tenantRepo.GetByIdAsync(
            user.TenantId, cancellationToken);

        if (tenant is null || !tenant.IsActive)
            return Result.Failure<AuthResponseDto>(IdentityErrors.Tenant.Inactive);

        // 4. Rotate refresh token — revoke old, issue new
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        var newRefreshTokenEntity = new IdentityService.Domain.Entities.RefreshToken(
            user.Id,
            newRefreshToken,
            newRefreshTokenExpiry);

        // AddRefreshToken internally revokes all existing tokens
        user.AddRefreshToken(newRefreshTokenEntity);

        await _userRepo.UpdateAsync(user, cancellationToken);

        // 5. Generate new access token
        var newAccessToken = _jwtService.GenerateAccessToken(user, tenant);

        return Result.Success(new AuthResponseDto(
            newAccessToken,
            newRefreshToken,
            _jwtService.GetAccessTokenExpiry(),
            new UserDto(
                user.Id,
                user.TenantId,
                user.Email,
                user.FullName,
                user.Role.ToString())));
    }
}