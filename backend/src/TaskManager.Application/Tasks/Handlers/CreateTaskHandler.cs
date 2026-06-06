using TaskManager.Application.DTOs;
using TaskManager.Application.Tasks.Commands;
using TaskManager.Domain.Interfaces;
using DomainTask = TaskManager.Domain.Entities.Task;

namespace TaskManager.Application.Tasks.Handlers;

public class CreateTaskHandler
{
    private readonly ITaskRepository _taskRepository;

    public CreateTaskHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<TaskDto> Handle(CreateTaskCommand command, CancellationToken ct = default)
    {
        var task = DomainTask.Create(
            command.Title,
            command.Description,
            command.Status,
            command.DueDate,
            command.UserId);

        var created = await _taskRepository.AddAsync(task, ct);
        
        return MapToDto(created);
    }

    internal static TaskDto MapToDto(DomainTask task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status.ToString(),
        task.DueDate,
        task.UserId,
        task.CreatedAt,
        task.UpdatedAt);
}
