namespace TaskManager.Domain.Events;

public record TaskCreatedEvent(Guid TaskId, Guid UserId, DateTime OccurredAt);
