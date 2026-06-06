namespace TaskManager.Application.Tasks.Commands;

public record DeleteTaskCommand(Guid Id, Guid UserId);
