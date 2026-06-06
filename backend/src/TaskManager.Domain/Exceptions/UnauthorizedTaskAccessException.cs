namespace TaskManager.Domain.Exceptions;

public class UnauthorizedTaskAccessException : DomainException
{
    public UnauthorizedTaskAccessException(Guid taskId, Guid userId)
        : base($"User '{userId}' is not authorized to access task '{taskId}'.") { }
}
