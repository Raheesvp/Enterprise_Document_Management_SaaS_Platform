
namespace IdentityService.Application.DTOs;


public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    UserDto User);

public record UserDto(
    Guid UserId,
    Guid TenantId,
    string Email,
    string FullName,
    string Role);

public record TenantDto(
    Guid TenantId,
    string Name,
    string Subdomain,
    string ContactEmail);