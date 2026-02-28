
using System.Reflection.Metadata;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user,Tenant tenant);

    string GenerateRefreshToken();

    DateTime GetAccessTokenExpiry();

    DateTime GetRefreshTokenExpiry();
}