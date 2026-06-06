using TaskManager.Application.DTOs;
using TaskManager.Application.Tasks.Queries;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Tasks.Handlers;

public class GetPagedTasksHandler
{
    private readonly ITaskRepository _taskRepository;

    public GetPagedTasksHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<PagedResult<TaskDto>> Handle(GetPagedTasksQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _taskRepository.GetPagedByUserIdAsync(query.UserId, query.Page, query.PageSize, ct);

        var dtos = items.Select(CreateTaskHandler.MapToDto);
        
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedResult<TaskDto>(dtos, totalCount, query.Page, query.PageSize, totalPages);
    }
}
