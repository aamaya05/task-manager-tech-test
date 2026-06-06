namespace TaskManager.Application.Tasks.Queries;

public record GetTaskByIdQuery(Guid Id, Guid UserId);
