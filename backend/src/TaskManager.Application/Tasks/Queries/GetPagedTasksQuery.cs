namespace TaskManager.Application.Tasks.Queries;

public record GetPagedTasksQuery(Guid UserId, int Page, int PageSize);
