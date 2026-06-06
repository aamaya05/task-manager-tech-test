using FluentAssertions;
using Moq;
using TaskManager.Application.Tasks.Handlers;
using TaskManager.Application.Tasks.Queries;
using TaskManager.Domain.Interfaces;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Application.Tests.Tasks;

public class GetPagedTasksHandlerTests
{
    private readonly Mock<ITaskRepository> _taskRepo = new();
    private readonly GetPagedTasksHandler _handler;

    public GetPagedTasksHandlerTests()
    {
        _handler = new GetPagedTasksHandler(_taskRepo.Object);
    }

    [Fact]
    public async Task GetPagedTasksHandler_Handle_WithValidQuery_ReturnsPagedResultWithCorrectShape()
    {
        var userId = Guid.NewGuid();
        var allTasks = Enumerable.Range(1, 3)
            .Select(i => DomainTask.Create($"Task {i}", null, DomainTaskStatus.Todo, null, userId))
            .ToList();

        _taskRepo.Setup(r => r.GetPagedByUserIdAsync(userId, 1, 10, default))
            .ReturnsAsync((allTasks, 3));

        var query = new GetPagedTasksQuery(userId, Page: 1, PageSize: 10);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedTasksHandler_Handle_WithMultiplePages_CalculatesTotalPagesCorrectly()
    {
        var tenTasks = Enumerable.Range(1, 10)
            .Select(i => DomainTask.Create($"Task {i}", null, DomainTaskStatus.Todo, null, Guid.NewGuid()))
            .ToList();

        _taskRepo.Setup(r => r.GetPagedByUserIdAsync(It.IsAny<Guid>(), 2, 10, default))
            .ReturnsAsync((tenTasks, 47));

        var query = new GetPagedTasksQuery(Guid.NewGuid(), Page: 2, PageSize: 10);

        var result = await _handler.Handle(query, default);

        result.TotalCount.Should().Be(47);
        result.TotalPages.Should().Be(5);
        result.Page.Should().Be(2);
        result.Items.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetPagedTasksHandler_Handle_CallsRepositoryWithExactPageAndPageSize()
    {
        _taskRepo.Setup(r => r.GetPagedByUserIdAsync(It.IsAny<Guid>(), 3, 25, default))
            .ReturnsAsync((new List<DomainTask>(), 0));

        var query = new GetPagedTasksQuery(Guid.NewGuid(), Page: 3, PageSize: 25);

        await _handler.Handle(query, default);

        _taskRepo.Verify(r => r.GetPagedByUserIdAsync(It.IsAny<Guid>(), 3, 25, default), Times.Once);
    }

    [Fact]
    public async Task GetPagedTasksHandler_Handle_WhenUserHasNoTasks_ReturnsEmptyPagedResult()
    {
        _taskRepo.Setup(r => r.GetPagedByUserIdAsync(It.IsAny<Guid>(), 1, 10, default))
            .ReturnsAsync((new List<DomainTask>(), 0));

        var result = await _handler.Handle(new GetPagedTasksQuery(Guid.NewGuid(), 1, 10), default);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
