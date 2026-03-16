namespace NotificationService.Application.DTOs;

public record NotificationDto(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string Title,
    string Message,
    string Type,
    string Status,
    Guid? ReferenceId,
    string? ReferenceType,
    DateTime CreatedAt,
    DateTime? ReadAt);
