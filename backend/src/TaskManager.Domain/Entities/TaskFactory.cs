using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Domain.Entities;

public static class TaskFactory
{
    public static Task Reconstitute(
        Guid id,
        string title,
        string? description,
        DomainTaskStatus status,
        DateTime? dueDate,
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return Task.Reconstitute(id, title, description, status, dueDate, userId, createdAt, updatedAt);
    }
}
