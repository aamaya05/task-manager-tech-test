using FluentAssertions;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Domain.Tests.ValueObjects;

public class TaskStatusValueObjectTests
{
    [Fact]
    public void TaskStatus_AllThreeValues_AreValid()
    {
        Enum.IsDefined(typeof(DomainTaskStatus), DomainTaskStatus.Todo).Should().BeTrue();
        Enum.IsDefined(typeof(DomainTaskStatus), DomainTaskStatus.InProgress).Should().BeTrue();
        Enum.IsDefined(typeof(DomainTaskStatus), DomainTaskStatus.Done).Should().BeTrue();
    }

    [Fact]
    public void TaskStatus_ParseFromString_ValidValues_ReturnCorrectStatus()
    {
        Enum.Parse<DomainTaskStatus>("Todo").Should().Be(DomainTaskStatus.Todo);
        Enum.Parse<DomainTaskStatus>("InProgress").Should().Be(DomainTaskStatus.InProgress);
        Enum.Parse<DomainTaskStatus>("Done").Should().Be(DomainTaskStatus.Done);
    }

    [Fact]
    public void TaskStatus_ParseFromString_InvalidValue_ThrowsArgumentException()
    {
        Action act = () => Enum.Parse<DomainTaskStatus>("InvalidStatus", ignoreCase: false);

        act.Should().Throw<ArgumentException>();
    }
}
