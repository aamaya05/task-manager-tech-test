namespace TaskManager.Domain.Exceptions;

public class TaskNotFoundException : DomainException
{
    public TaskNotFoundException(Guid id)
        : base($"Task with ID '{id}' was not found.") { }
}
