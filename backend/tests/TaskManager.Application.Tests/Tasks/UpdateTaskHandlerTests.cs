using FluentAssertions;
using Moq;
using TaskManager.Application.Tasks.Commands;
using TaskManager.Application.Tasks.Handlers;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Application.Tests.Tasks;

public class UpdateTaskHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly UpdateTaskHandler _handler;

    public UpdateTaskHandlerTests()
    {
        _handler = new UpdateTaskHandler(_taskRepo.Object);
    }

    [Fact]
    public async Task UpdateTaskHandler_Handle_WithValidCommand_UpdatesAndReturnsTaskDto()
    {
        var userId = Guid.NewGuid();
        var existing = DomainTask.Create("Old", null, DomainTaskStatus.Todo, null, userId);
        _taskRepo.Setup(r => r.GetByIdAsync(existing.Id, default)).ReturnsAsync(existing);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<DomainTask>(), default)).Returns(System.Threading.Tasks.Task.CompletedTask);
        var command = new UpdateTaskCommand(existing.Id, "New Title", null, DomainTaskStatus.Done, null, userId);

        var result = await _handler.Handle(command, default);

        result.Title.Should().Be("New Title");
        result.Status.Should().Be("Done");
        _taskRepo.Verify(r => r.UpdateAsync(It.IsAny<DomainTask>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskHandler_Handle_WhenTaskNotFound_ThrowsTaskNotFoundException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((DomainTask?)null);

        Func<Task> act = () => _handler.Handle(
            new UpdateTaskCommand(Guid.NewGuid(), "X", null, DomainTaskStatus.Todo, null, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<TaskNotFoundException>();
        _taskRepo.Verify(r => r.UpdateAsync(It.IsAny<DomainTask>(), default), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskHandler_Handle_WhenTaskBelongsToDifferentUser_ThrowsUnauthorizedTaskAccessException()
    {
        var existing = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, Guid.NewGuid());
        _taskRepo.Setup(r => r.GetByIdAsync(existing.Id, default)).ReturnsAsync(existing);
        var command = new UpdateTaskCommand(existing.Id, "X", null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        Func<Task> act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<UnauthorizedTaskAccessException>();
    }
}
