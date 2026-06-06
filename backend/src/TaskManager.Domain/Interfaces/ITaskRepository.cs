namespace TaskManager.Domain.Interfaces;

public interface ITaskRepository
{
    Task<Entities.Task?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Entities.Task> Items, int TotalCount)> GetPagedByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<Entities.Task> AddAsync(Entities.Task task, CancellationToken ct = default);
    Task UpdateAsync(Entities.Task task, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
