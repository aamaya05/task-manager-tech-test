using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Application.Tasks.Commands;

public record CreateTaskCommand(
    string Title,
    string? Description,
    DomainTaskStatus Status,
    DateTime? DueDate,
    Guid UserId);
