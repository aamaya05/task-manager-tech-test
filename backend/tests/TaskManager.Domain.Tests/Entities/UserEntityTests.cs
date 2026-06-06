using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Events;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Domain.Tests.Entities;

public class UserEntityTests
{
    [Fact]
    public void User_Create_WithValidArguments_ReturnsUserWithCorrectProperties()
    {
        var username = "john_doe";
        var email = new Email("john@example.com");
        var passwordHash = "$2b$12$abc...";

        var user = User.Create(username, email, passwordHash);

        user.Id.Should().NotBe(Guid.Empty);
        user.Username.Should().Be(username);
        user.Email.Value.Should().Be("john@example.com");
        user.PasswordHash.Should().Be(passwordHash);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void User_Create_WithEmptyUsername_ThrowsDomainException()
    {
        Action act = () => User.Create("", new Email("a@b.com"), "hash");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void User_Create_WithUsernameShorterThan3Chars_ThrowsDomainException()
    {
        Action act = () => User.Create("ab", new Email("a@b.com"), "hash");

        act.Should().Throw<DomainException>().WithMessage("*3*");
    }

    [Fact]
    public void User_Create_WithUsernameExceeding50Chars_ThrowsDomainException()
    {
        var longUsername = new string('x', 51);

        Action act = () => User.Create(longUsername, new Email("a@b.com"), "hash");

        act.Should().Throw<DomainException>().WithMessage("*50*");
    }

    [Fact]
    public void User_Create_WithEmptyPasswordHash_ThrowsDomainException()
    {
        Action act = () => User.Create("john", new Email("a@b.com"), "");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void User_Create_RaisesUserRegisteredEventWithCorrectData()
    {
        var user = User.Create("john", new Email("john@example.com"), "hash");

        user.DomainEvents.Should().ContainSingle(e => e is UserRegisteredEvent);
        var evt = (UserRegisteredEvent)user.DomainEvents.First();
        evt.UserId.Should().Be(user.Id);
        evt.Email.Should().Be("john@example.com");
    }
}
