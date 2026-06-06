using TaskManager.Application.Tasks.Commands;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Application.Tasks.Handlers;

public class DeleteTaskHandler
{
    private readonly ITaskRepository _taskRepository;

    public DeleteTaskHandler(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task Handle(DeleteTaskCommand command, CancellationToken ct = default)
    {
        var task = await _taskRepository.GetByIdAsync(command.Id, ct) ?? throw new TaskNotFoundException(command.Id);

        if (!task.BelongsTo(command.UserId))
        {
            throw new UnauthorizedTaskAccessException(command.Id, command.UserId);
        }

        await _taskRepository.DeleteAsync(command.Id, ct);
    }
}
