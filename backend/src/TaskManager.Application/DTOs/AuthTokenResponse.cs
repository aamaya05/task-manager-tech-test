namespace TaskManager.Application.DTOs;

public record AuthTokenResponse(
    string Token,
    DateTime ExpiresAt,
    Guid UserId,
    string Username);
