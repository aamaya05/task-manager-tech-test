using TaskManager.Domain.Events;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Domain.Entities;

public class User
{
    private readonly List<object> _domainEvents = new();
    private const int MIN_CHARACTERS = 3;
    private const int MAX_CHARACTERS = 50;
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private User() { }

    public static User Create(string username, Email email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username must not be null or empty.");
        }

        if (username.Length < MIN_CHARACTERS)
        {
            throw new DomainException($"Username must be at least {MIN_CHARACTERS} characters long.");
        }

        if (username.Length > MAX_CHARACTERS)
        {
            throw new DomainException($"Username must not exceed {MAX_CHARACTERS} characters.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash must not be null or empty.");
        }

        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = now
        };

        user._domainEvents.Add(new UserRegisteredEvent(user.Id, email.Value, now));
    
        return user;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    internal static User Reconstitute(Guid id, string username, Email email, string passwordHash, DateTime createdAt)
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = createdAt
        };
    }
}
