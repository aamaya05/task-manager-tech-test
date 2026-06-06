using FluentAssertions;
using Moq;
using TaskManager.Application.Tasks.Commands;
using TaskManager.Application.Tasks.Handlers;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Application.Tests.Tasks;

public class DeleteTaskHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly DeleteTaskHandler _handler;

    public DeleteTaskHandlerTests()
    {
        _handler = new DeleteTaskHandler(_taskRepo.Object);
    }

    [Fact]
    public async Task DeleteTaskHandler_Handle_WithValidCommand_DeletesTask()
    {
        var userId = Guid.NewGuid();
        var task = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, userId);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);
        _taskRepo.Setup(r => r.DeleteAsync(task.Id, default)).Returns(System.Threading.Tasks.Task.CompletedTask);

        await _handler.Handle(new DeleteTaskCommand(task.Id, userId), default);

        _taskRepo.Verify(r => r.DeleteAsync(task.Id, default), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskHandler_Handle_WhenTaskNotFound_ThrowsTaskNotFoundException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((DomainTask?)null);

        Func<Task> act = () => _handler.Handle(
            new DeleteTaskCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<TaskNotFoundException>();
        _taskRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task DeleteTaskHandler_Handle_WhenTaskBelongsToDifferentUser_ThrowsUnauthorizedTaskAccessException()
    {
        var task = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, Guid.NewGuid());
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);

        Func<Task> act = () => _handler.Handle(
            new DeleteTaskCommand(task.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<UnauthorizedTaskAccessException>();
    }
}
