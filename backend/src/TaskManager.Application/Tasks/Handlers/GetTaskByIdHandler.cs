using TaskManager.Application.DTOs;
using TaskManager.Application.Tasks.Queries;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Tasks.Handlers;

public class GetTaskByIdHandler
{
    private readonly ITaskRepository _taskRepository;

    public GetTaskByIdHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskDto> Handle(GetTaskByIdQuery query, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(query.Id, ct) ?? throw new TaskNotFoundException(query.Id);

        if (!task.BelongsTo(query.UserId))
        {
            throw new UnauthorizedTaskAccessException(query.Id, query.UserId);
        }

        return CreateTaskHandler.MapToDto(task);
    }
}
