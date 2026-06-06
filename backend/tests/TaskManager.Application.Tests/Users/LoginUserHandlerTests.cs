using FluentAssertions;
using Moq;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.Handlers;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Application.Tests.Users;

public class LoginUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwtService = new();
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _handler = new LoginUserHandler(_userRepo.Object, _hasher.Object, _jwtService.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task LoginUserHandler_Handle_WithCorrectCredentials_ReturnsAuthTokenResponse()
    {
        var user = User.Create("john", new Email("john@example.com"), "$2b$12$hash");
        _userRepo.Setup(r => r.GetByEmailAsync("john@example.com", default)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("SecurePass1!", "$2b$12$hash")).Returns(true);
        _jwtService.Setup(j => j.GenerateToken(user)).Returns("eyJhbGci...");
        _jwtService.Setup(j => j.GetExpiry("eyJhbGci...")).Returns(DateTime.UtcNow.AddHours(1));

        var result = await _handler.Handle(new LoginUserCommand("john@example.com", "SecurePass1!"), default);

        result.Should().NotBeNull();
        result!.Token.Should().Be("eyJhbGci...");
        result.UserId.Should().Be(user.Id);
        result.Username.Should().Be("john");
    }

    [Fact]
    public async System.Threading.Tasks.Task LoginUserHandler_Handle_WhenEmailNotFound_ReturnsNull()
    {
        _userRepo.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var result = await _handler.Handle(new LoginUserCommand("x@x.com", "pass"), default);

        result.Should().BeNull();
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task LoginUserHandler_Handle_WhenPasswordDoesNotMatch_ReturnsNull()
    {
        var user = User.Create("john", new Email("john@example.com"), "$2b$12$hash");
        _userRepo.Setup(r => r.GetByEmailAsync("john@example.com", default)).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("wrongpass", "$2b$12$hash")).Returns(false);

        var result = await _handler.Handle(new LoginUserCommand("john@example.com", "wrongpass"), default);

        result.Should().BeNull();
        _jwtService.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
