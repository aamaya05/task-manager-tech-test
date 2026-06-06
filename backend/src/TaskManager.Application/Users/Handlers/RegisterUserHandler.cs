using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.Commands;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Application.Users.Handlers;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> Handle(RegisterUserCommand command, CancellationToken ct = default)
    {
        if (await _userRepository.EmailExistsAsync(command.Email, ct))
        {
            throw new DuplicateEmailException(command.Email);
        }

        var email = new Email(command.Email);

        var passwordHash = _passwordHasher.Hash(command.Password);

        var user = User.Create(command.Username, email, passwordHash);

        var created = await _userRepository.AddAsync(user, ct);
        
        return new UserDto(created.Id, created.Username, created.Email.Value, created.CreatedAt);
    }
}
