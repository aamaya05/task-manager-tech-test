namespace TaskManager.Domain.Exceptions;

public class InvalidEmailException : DomainException
{
    public InvalidEmailException(string? email)
        : base($"'{email}' is not a valid email address.") { }
}
