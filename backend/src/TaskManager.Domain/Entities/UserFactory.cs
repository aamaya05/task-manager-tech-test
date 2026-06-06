using TaskManager.Domain.ValueObjects;

namespace TaskManager.Domain.Entities;

/// <summary>
/// Reconstitutes a User entity from persisted data without raising domain events.
/// Used exclusively by the Infrastructure layer for mapping database rows.
/// </summary>
public static class UserFactory
{
    public static User Reconstitute(Guid id, string username, Email email, string passwordHash, DateTime createdAt)
    {
        return User.Reconstitute(id, username, email, passwordHash, createdAt);
    }
}
