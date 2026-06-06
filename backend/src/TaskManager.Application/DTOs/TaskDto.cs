namespace TaskManager.Application.DTOs;

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    DateTime? DueDate,
    Guid UserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
