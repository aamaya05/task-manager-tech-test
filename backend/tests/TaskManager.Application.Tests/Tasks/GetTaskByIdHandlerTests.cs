using FluentAssertions;
using Moq;
using TaskManager.Application.Tasks.Handlers;
using TaskManager.Application.Tasks.Queries;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Application.Tests.Tasks;

public class GetTaskByIdHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly GetTaskByIdHandler _handler;

    public GetTaskByIdHandlerTests()
    {
        _handler = new GetTaskByIdHandler(_taskRepo.Object);
    }

    [Fact]
    public async Task GetTaskByIdHandler_Handle_WhenTaskExistsAndBelongsToUser_ReturnsTaskDto()
    {
        var userId = Guid.NewGuid();
        var task = DomainTask.Create("My Task", "Desc", DomainTaskStatus.InProgress, null, userId);
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);

        var result = await _handler.Handle(new GetTaskByIdQuery(task.Id, userId), default);

        result.Should().NotBeNull();
        result.Id.Should().Be(task.Id);
        result.Title.Should().Be("My Task");
    }

    [Fact]
    public async Task GetTaskByIdHandler_Handle_WhenTaskNotFound_ThrowsTaskNotFoundException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((DomainTask?)null);

        Func<Task> act = () => _handler.Handle(
            new GetTaskByIdQuery(Guid.NewGuid(), Guid.NewGuid()), default);

        await act.Should().ThrowAsync<TaskNotFoundException>();
    }

    [Fact]
    public async Task GetTaskByIdHandler_Handle_WhenTaskBelongsToDifferentUser_ThrowsUnauthorizedTaskAccessException()
    {
        var task = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, Guid.NewGuid());
        _taskRepo.Setup(r => r.GetByIdAsync(task.Id, default)).ReturnsAsync(task);

        Func<Task> act = () => _handler.Handle(
            new GetTaskByIdQuery(task.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<UnauthorizedTaskAccessException>();
    }
}
