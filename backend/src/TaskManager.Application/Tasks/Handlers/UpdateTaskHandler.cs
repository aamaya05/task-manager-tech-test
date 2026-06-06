using TaskManager.Application.DTOs;
using TaskManager.Application.Tasks.Commands;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Tasks.Handlers;

public class UpdateTaskHandler
{
    private readonly ITaskRepository _taskRepository;

    public UpdateTaskHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async System.Threading.Tasks.Task<TaskDto> Handle(UpdateTaskCommand command, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(command.Id, ct) ?? throw new TaskNotFoundException(command.Id);

        if (!task.BelongsTo(command.UserId))
        {
            throw new UnauthorizedTaskAccessException(command.Id, command.UserId);
        }

        task.Update(command.Title, command.Description, command.Status, command.DueDate);
        
        await _taskRepository.UpdateAsync(task, ct);

        return CreateTaskHandler.MapToDto(task);
    }
}
