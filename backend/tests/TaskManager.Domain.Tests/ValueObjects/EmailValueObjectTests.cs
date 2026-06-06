using FluentAssertions;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Domain.Tests.ValueObjects;

public class EmailValueObjectTests
{
    [Fact]
    public void Email_Constructor_WithValidEmail_SetsValue()
    {
        var email = new Email("user@domain.com");

        email.Value.Should().Be("user@domain.com");
    }

    [Fact]
    public void Email_Constructor_WithMissingAt_ThrowsInvalidEmailException()
    {
        Action act = () => new Email("notanemail");

        act.Should().Throw<InvalidEmailException>();
    }

    [Fact]
    public void Email_Constructor_WithEmptyString_ThrowsInvalidEmailException()
    {
        Action act = () => new Email("");

        act.Should().Throw<InvalidEmailException>();
    }

    [Fact]
    public void Email_Constructor_WithNullValue_ThrowsInvalidEmailException()
    {
        Action act = () => new Email(null!);

        act.Should().Throw<InvalidEmailException>();
    }

    [Fact]
    public void Email_Equality_TwoEmailsWithSameValue_AreEqual()
    {
        var e1 = new Email("user@example.com");
        var e2 = new Email("user@example.com");

        e1.Should().Be(e2);
        (e1 == e2).Should().BeTrue();
    }

    [Fact]
    public void Email_Equality_TwoEmailsWithDifferentValues_AreNotEqual()
    {
        var e1 = new Email("a@example.com");
        var e2 = new Email("b@example.com");

        e1.Should().NotBe(e2);
    }
}
