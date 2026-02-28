using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Domain.Errors;
using IdentityService.Domain.Repositories;
using MediatR;
using Shared.Domain.Common;

namespace IdentityService.Application.Commands.RegisterTenant;

public sealed class RegisterTenantCommandHandler
    : IRequestHandler<RegisterTenantCommand, Result<TenantDto>>
{
    private readonly ITenantRepository _tenantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPasswordService _passwordService;

    public RegisterTenantCommandHandler(
        ITenantRepository tenantRepo,
        IUserRepository userRepo,
        IPasswordService passwordService)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
        _passwordService = passwordService;
    }

    public async Task<Result<TenantDto>> Handle(
        RegisterTenantCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Check subdomain uniqueness
        var subdomainTaken = await _tenantRepo.SubdomainExistsAsync(
            command.Subdomain, cancellationToken);

        if (subdomainTaken)
            return Result.Failure<TenantDto>(IdentityErrors.Tenant.SubdomainTaken);

        // 2. Create Tenant aggregate
        var tenant = Tenant.Create(
            command.TenantName,
            command.Subdomain,
            command.ContactEmail);

        await _tenantRepo.AddAsync(tenant, cancellationToken);

        // 3. Create Admin user for this tenant
        var passwordHash = _passwordService.HashPassword(command.AdminPassword);

        var adminUser = User.Create(
            tenant.Id,
            command.AdminEmail,
            command.AdminFullName,
            passwordHash,
            UserRole.Admin);

        await _userRepo.AddAsync(adminUser, cancellationToken);

        // 4. Return tenant info
        return Result.Success(new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.ContactEmail));
    }
}