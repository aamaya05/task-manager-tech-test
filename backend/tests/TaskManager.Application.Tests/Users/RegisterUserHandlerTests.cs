using FluentAssertions;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.Handlers;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Application.Tests.Users;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(_userRepo.Object, _hasher.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task RegisterUserHandler_Handle_WithValidCommand_CreatesAndReturnsUserDto()
    {
        _userRepo.Setup(r => r.EmailExistsAsync("john@example.com", default)).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("SecurePass1!")).Returns("$2b$12$hash");
        var createdUser = User.Create("john_doe", new Email("john@example.com"), "$2b$12$hash");
        _userRepo.Setup(r => r.AddAsync(It.IsAny<User>(), default)).ReturnsAsync(createdUser);

        var command = new RegisterUserCommand("john_doe", "john@example.com", "SecurePass1!");
        var result = await _handler.Handle(command, default);

        result.Username.Should().Be("john_doe");
        result.Email.Should().Be("john@example.com");
        _hasher.Verify(h => h.Hash("SecurePass1!"), Times.Once);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task RegisterUserHandler_Handle_WhenEmailAlreadyExists_ThrowsDuplicateEmailException()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(true);

        Func<System.Threading.Tasks.Task> act = () => _handler.Handle(
            new RegisterUserCommand("user", "taken@example.com", "Pass123!"), default);

        await act.Should().ThrowAsync<DuplicateEmailException>();
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task RegisterUserHandler_Handle_PlainPasswordNeverPassedToRepository()
    {
        _userRepo.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), default)).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("plaintext")).Returns("$2b$HASH");
        User? captured = null;
        _userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), default))
            .Callback<User, CancellationToken>((u, _) => captured = u)
            .ReturnsAsync((User u, CancellationToken _) => u);

        await _handler.Handle(new RegisterUserCommand("unit test", "a@b.com", "plaintext"), default);

        captured!.PasswordHash.Should().NotBe("plaintext");
        captured.PasswordHash.Should().Be("$2b$HASH");
    }
}
