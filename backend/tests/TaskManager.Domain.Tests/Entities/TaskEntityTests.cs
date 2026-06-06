using FluentAssertions;
using TaskManager.Domain.Events;
using TaskManager.Domain.Exceptions;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Domain.Tests.Entities;

public class TaskEntityTests
{
    [Fact]
    public void Task_Create_WithValidArguments_ReturnsTaskWithCorrectProperties()
    {
        var title = "Write unit tests";
        var description = "Use xUnit and FluentAssertions";
        var status = DomainTaskStatus.Todo;
        var dueDate = DateTime.UtcNow.AddDays(7);
        var userId = Guid.NewGuid();

        var task = DomainTask.Create(title, description, status, dueDate, userId);

        task.Id.Should().NotBe(Guid.Empty);
        task.Title.Should().Be(title);
        task.Description.Should().Be(description);
        task.Status.Should().Be(status);
        task.DueDate.Should().Be(dueDate);
        task.UserId.Should().Be(userId);
        task.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        task.UpdatedAt.Should().Be(task.CreatedAt);
    }

    [Fact]
    public void Task_Create_WithEmptyTitle_ThrowsDomainException()
    {
        Action act = () => DomainTask.Create("", null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*title*");
    }

    [Fact]
    public void Task_Create_WithNullTitle_ThrowsDomainException()
    {
        Action act = () => DomainTask.Create(null!, null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Task_Create_WithTitleExceeding200Characters_ThrowsDomainException()
    {
        var title = new string('A', 201);

        Action act = () => DomainTask.Create(title, null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*200*");
    }

    [Fact]
    public void Task_Create_WithEmptyUserId_ThrowsDomainException()
    {
        Action act = () => DomainTask.Create("Valid Title", null, DomainTaskStatus.Todo, null, Guid.Empty);

        act.Should().Throw<DomainException>().WithMessage("*user*");
    }

    [Fact]
    public void Task_Create_CalledTwice_GeneratesDistinctIds()
    {
        var task1 = DomainTask.Create("Task 1", null, DomainTaskStatus.Todo, null, Guid.NewGuid());
        var task2 = DomainTask.Create("Task 2", null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        task1.Id.Should().NotBe(task2.Id);
    }

    [Fact]
    public void Task_Update_WithValidArguments_UpdatesFieldsAndSetsUpdatedAt()
    {
        var task = DomainTask.Create("Original", "Desc", DomainTaskStatus.Todo, null, Guid.NewGuid());
        var originalUpdatedAt = task.UpdatedAt;
        Thread.Sleep(10);

        task.Update("Updated Title", "New Desc", DomainTaskStatus.InProgress, DateTime.UtcNow.AddDays(3));

        task.Title.Should().Be("Updated Title");
        task.Description.Should().Be("New Desc");
        task.Status.Should().Be(DomainTaskStatus.InProgress);
        task.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Task_Update_WithEmptyTitle_ThrowsDomainException()
    {
        var task = DomainTask.Create("Original", null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        Action act = () => task.Update("", null, DomainTaskStatus.Todo, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Task_Update_DoesNotChangeIdOrUserIdOrCreatedAt()
    {
        var userId = Guid.NewGuid();
        var task = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, userId);
        var originalId = task.Id;
        var originalUserId = task.UserId;
        var originalCreatedAt = task.CreatedAt;

        task.Update("New Title", null, DomainTaskStatus.Done, null);

        task.Id.Should().Be(originalId);
        task.UserId.Should().Be(originalUserId);
        task.CreatedAt.Should().Be(originalCreatedAt);
    }

    [Fact]
    public void Task_BelongsTo_WhenUserIdMatches_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var task = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, userId);

        task.BelongsTo(userId).Should().BeTrue();
    }

    [Fact]
    public void Task_BelongsTo_WhenUserIdDoesNotMatch_ReturnsFalse()
    {
        var task = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, Guid.NewGuid());

        task.BelongsTo(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Task_Create_RaisesTaskCreatedEventWithCorrectData()
    {
        var userId = Guid.NewGuid();

        var task = DomainTask.Create("Task", null, DomainTaskStatus.Todo, null, userId);

        task.DomainEvents.Should().ContainSingle(e => e is TaskCreatedEvent);
        var evt = (TaskCreatedEvent)task.DomainEvents.First();
        evt.TaskId.Should().Be(task.Id);
        evt.UserId.Should().Be(userId);
        evt.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
