using FluentAssertions;
using Moq;
using TaskManager.Application.Tasks.Commands;
using TaskManager.Application.Tasks.Handlers;
using TaskManager.Domain.Exceptions;
using TaskManager.Domain.Interfaces;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Application.Tests.Tasks;

public class CreateTaskHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly CreateTaskHandler _handler;

    public CreateTaskHandlerTests()
    {
        _handler = new CreateTaskHandler(_taskRepo.Object);
    }

    [Fact]
    public async Task CreateTaskHandler_Handle_WithValidCommand_ReturnsTaskDto()
    {
        var userId = Guid.NewGuid();
        var command = new CreateTaskCommand("Write tests", "Details", DomainTaskStatus.Todo, null, userId);
        var created = DomainTask.Create(command.Title, command.Description, command.Status, command.DueDate, command.UserId);

        _taskRepo.Setup(r => r.AddAsync(It.IsAny<DomainTask>(), default)).ReturnsAsync(created);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Write tests");
        result.UserId.Should().Be(userId);
        _taskRepo.Verify(r => r.AddAsync(It.IsAny<DomainTask>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateTaskHandler_Handle_WithEmptyTitle_ThrowsDomainException()
    {
        var command = new CreateTaskCommand("", null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        Func<Task> act = () => _handler.Handle(command, default);

        await act.Should().ThrowAsync<DomainException>();
        
        _taskRepo.Verify(r => r.AddAsync(It.IsAny<DomainTask>(), default), Times.Never);
    }
}
