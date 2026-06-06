using TaskManager.Domain.Entities;

namespace TaskManager.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    DateTime GetExpiry(string token);
}
