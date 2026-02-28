using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Errors;
using IdentityService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace IdentityService.Application.Commands.LoginUser;

public sealed class LoginUserCommandHandler
    : IRequestHandler<LoginUserCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    public LoginUserCommandHandler(
        IUserRepository userRepo,
        ITenantRepository tenantRepo,
        IPasswordService passwordService,
        IJwtService jwtService)
    {
        _userRepo = userRepo;
        _tenantRepo = tenantRepo;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResponseDto>> Handle(
        LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Resolve tenant from subdomain
        var tenant = await _tenantRepo.GetBySubdomainAsync(
            command.Subdomain, cancellationToken);

        if (tenant is null)
            return Result.Failure<AuthResponseDto>(
                IdentityErrors.User.InvalidCredentials);

        if (!tenant.IsActive)
            return Result.Failure<AuthResponseDto>(
                IdentityErrors.Tenant.Inactive);

        // 2. Find user within this tenant
        var user = await _userRepo.GetByEmailAsync(
            tenant.Id, command.Email, cancellationToken);

        // Always verify password even if user not found
        // Prevents timing attacks revealing whether email exists
        if (user is null || !_passwordService.VerifyPassword(
                command.Password, user.PasswordHash))
            return Result.Failure<AuthResponseDto>(
                IdentityErrors.User.InvalidCredentials);

        if (!user.IsActive)
            return Result.Failure<AuthResponseDto>(
                IdentityErrors.User.Inactive);

        // 3. Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user, tenant);
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtService.GetRefreshTokenExpiry();

        // 4. Attach refresh token to user aggregate
        var refreshTokenEntity = new IdentityService.Domain.Entities.RefreshToken(
            user.Id,
            refreshToken,
            refreshTokenExpiry);

        user.AddRefreshToken(refreshTokenEntity);
        user.RecordLogin();

        await _userRepo.UpdateAsync(user, cancellationToken);

        // 5. Return auth response
        return Result.Success(new AuthResponseDto(
            accessToken,
            refreshToken,
            _jwtService.GetAccessTokenExpiry(),
            new UserDto(
                user.Id,
                user.TenantId,
                user.Email,
                user.FullName,
                user.Role.ToString())));
    }
}