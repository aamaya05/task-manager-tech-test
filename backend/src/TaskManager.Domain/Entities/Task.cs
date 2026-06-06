using TaskManager.Domain.Events;
using TaskManager.Domain.Exceptions;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Domain.Entities;

public class Task
{
    private readonly List<object> _domainEvents = new();
    private const int MAX_CHARACTERS = 200;
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DomainTaskStatus Status { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private Task() { }

    public static Task Create(
        string title,
        string? description,
        DomainTaskStatus status,
        DateTime? dueDate,
        Guid userId)
    {
        ValidateTitle(title);

        if (userId == Guid.Empty)
        {
            throw new DomainException("The user ID must not be empty.");
        }

        if (dueDate.HasValue && dueDate.Value == default)
        {
            throw new DomainException("The due date must be a valid UTC datetime.");
        }

        var now = DateTime.UtcNow;

        var task = new Task
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description,
            Status = status,
            DueDate = dueDate,
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        task._domainEvents.Add(new TaskCreatedEvent(task.Id, userId, now));

        return task;
    }

    public void Update(string title, string? description, DomainTaskStatus status, DateTime? dueDate)
    {
        ValidateTitle(title);

        if (dueDate.HasValue && dueDate.Value == default)
        {
            throw new DomainException("The due date must be a valid UTC datetime.");
        }

        Title = title.Trim();
        Description = description;
        Status = status;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool BelongsTo(Guid userId) => UserId == userId;

    public void ClearDomainEvents() => _domainEvents.Clear();

    internal static Task Reconstitute(
        Guid id, string title, string? description, DomainTaskStatus status,
        DateTime? dueDate, Guid userId, DateTime createdAt, DateTime updatedAt)
    {
        return new Task
        {
            Id = id,
            Title = title,
            Description = description,
            Status = status,
            DueDate = dueDate,
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("The task title must not be null or empty.");
        }

        if (title.Length > MAX_CHARACTERS)
        {
            throw new DomainException($"The task title must not exceed {MAX_CHARACTERS} characters.");
        }
    }
}
