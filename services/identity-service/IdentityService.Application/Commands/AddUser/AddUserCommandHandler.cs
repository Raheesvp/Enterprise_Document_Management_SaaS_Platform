using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Errors;
using IdentityService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace IdentityService.Application.Commands.AddUser;

public sealed class AddUserCommandHandler
    : IRequestHandler<AddUserCommand, Result<UserCreatedDto>>
{
    private readonly IUserRepository   _userRepo;
    private readonly ITenantRepository _tenantRepo;
    private readonly IPasswordService  _passwordService;

    public AddUserCommandHandler(
        IUserRepository   userRepo,
        ITenantRepository tenantRepo,
        IPasswordService  passwordService)
    {
        _userRepo        = userRepo;
        _tenantRepo      = tenantRepo;
        _passwordService = passwordService;
    }

    public async Task<Result<UserCreatedDto>> Handle(
        AddUserCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Verify tenant exists and is active
        var tenant = await _tenantRepo.GetByIdAsync(
            command.TenantId, cancellationToken);

        if (tenant is null || !tenant.IsActive)
            return Result.Failure<UserCreatedDto>(
                IdentityErrors.Tenant.Inactive);

        // 2. Check email not already taken in this tenant
        var emailTaken = await _userRepo.EmailExistsAsync(
            command.TenantId,
            command.Email,
            cancellationToken);

        if (emailTaken)
            return Result.Failure<UserCreatedDto>(
                IdentityErrors.User.EmailTaken);

        // 3. Parse and validate role
        // Admin cannot be assigned via this endpoint
        var roleInput = command.Role == "Manager" ? "Manager" : command.Role;
        if (!Enum.TryParse<UserRole>(roleInput, out var role)
            || role == UserRole.Admin)
        {
            return Result.Failure<UserCreatedDto>(
                new Error("User.InvalidRole",
                    "Role must be Manager or Viewer"));
        }

        // 4. Hash password
        var passwordHash = _passwordService
            .HashPassword(command.Password);

        // 5. Create user using domain factory method
        var user = User.Create(
            command.TenantId,
            command.Email,
            command.FullName,
            passwordHash,
            role);

        await _userRepo.AddAsync(user, cancellationToken);

        return Result.Success(new UserCreatedDto(
            user.Id,
            user.Email,
            user.Role.ToString()));
    }
}
