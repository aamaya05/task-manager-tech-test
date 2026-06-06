using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.Commands;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Users.Handlers;

public class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthTokenResponse?> Handle(LoginUserCommand command, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(command.Email, ct);

        if (user is null) return null;

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash)) return null;

        var token = _jwtTokenService.GenerateToken(user);
        
        var expiresAt = _jwtTokenService.GetExpiry(token);

        return new AuthTokenResponse(token, expiresAt, user.Id, user.Username);
    }
}
