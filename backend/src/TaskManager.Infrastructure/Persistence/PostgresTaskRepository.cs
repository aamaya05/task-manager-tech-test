using Npgsql;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Infrastructure.Persistence;

public class PostgresTaskRepository : ITaskRepository
{
    private const string ColId = "id";
    private const string ColTitle = "title";
    private const string ColDescription = "description";
    private const string ColStatus = "status";
    private const string ColDueDate = "due_date";
    private const string ColUserId = "user_id";
    private const string ColCreatedAt = "created_at";
    private const string ColUpdatedAt = "updated_at";

    private readonly DbConnectionFactory _connectionFactory;

    public PostgresTaskRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Domain.Entities.Task?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"SELECT {ColId},{ColTitle},{ColDescription},{ColStatus},{ColDueDate},{ColUserId},{ColCreatedAt},{ColUpdatedAt} FROM tasks WHERE {ColId} = @id",
            conn);

        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct)) return null;

        return MapToEntity(reader);
    }

    public async Task<(IEnumerable<Domain.Entities.Task> Items, int TotalCount)> GetPagedByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var countCmd = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM tasks WHERE {ColUserId} = @userId", conn);
        countCmd.Parameters.AddWithValue("@userId", userId);

        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(ct));

        var offset = (page - 1) * pageSize;

        await using var dataCmd = new NpgsqlCommand(
            $"SELECT {ColId},{ColTitle},{ColDescription},{ColStatus},{ColDueDate},{ColUserId},{ColCreatedAt},{ColUpdatedAt} " +
            $"FROM tasks WHERE {ColUserId} = @userId " +
            $"ORDER BY {ColCreatedAt} DESC " +
            $"LIMIT @pageSize OFFSET @offset",
            conn);

        dataCmd.Parameters.AddWithValue("@userId", userId);
        dataCmd.Parameters.AddWithValue("@pageSize", pageSize);
        dataCmd.Parameters.AddWithValue("@offset", offset);

        await using var reader = await dataCmd.ExecuteReaderAsync(ct);

        var items = new List<Domain.Entities.Task>();

        while (await reader.ReadAsync(ct))
        {
            items.Add(MapToEntity(reader));
        }

        return (items, totalCount);
    }

    public async Task<Domain.Entities.Task> AddAsync(Domain.Entities.Task task, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"INSERT INTO tasks ({ColId},{ColTitle},{ColDescription},{ColStatus},{ColDueDate},{ColUserId},{ColCreatedAt},{ColUpdatedAt}) " +
            $"VALUES (@id,@title,@description,@status,@dueDate,@userId,@createdAt,@updatedAt) " +
            $"RETURNING {ColId},{ColTitle},{ColDescription},{ColStatus},{ColDueDate},{ColUserId},{ColCreatedAt},{ColUpdatedAt}",
            conn);

        AddTaskParameters(cmd, task);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        await reader.ReadAsync(ct);

        return MapToEntity(reader);
    }

    public async System.Threading.Tasks.Task UpdateAsync(Domain.Entities.Task task, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"UPDATE tasks SET {ColTitle}=@title,{ColDescription}=@description,{ColStatus}=@status," +
            $"{ColDueDate}=@dueDate,{ColUpdatedAt}=@updatedAt WHERE {ColId}=@id",
            conn);

        cmd.Parameters.AddWithValue("@id", task.Id);
        cmd.Parameters.AddWithValue("@title", task.Title);
        cmd.Parameters.AddWithValue("@description", (object?)task.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", task.Status.ToString());
        cmd.Parameters.AddWithValue("@dueDate", (object?)task.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@updatedAt", task.UpdatedAt);
    
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async System.Threading.Tasks.Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand($"DELETE FROM tasks WHERE {ColId} = @id", conn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand($"SELECT COUNT(1) FROM tasks WHERE {ColId} = @id", conn);

        cmd.Parameters.AddWithValue("@id", id);

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
    
        return count > 0;
    }

    private static void AddTaskParameters(NpgsqlCommand cmd, Domain.Entities.Task task)
    {
        cmd.Parameters.AddWithValue("@id", task.Id);
        cmd.Parameters.AddWithValue("@title", task.Title);
        cmd.Parameters.AddWithValue("@description", (object?)task.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@status", task.Status.ToString());
        cmd.Parameters.AddWithValue("@dueDate", (object?)task.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@userId", task.UserId);
        cmd.Parameters.AddWithValue("@createdAt", task.CreatedAt);
        cmd.Parameters.AddWithValue("@updatedAt", task.UpdatedAt);
    }

    private static Domain.Entities.Task MapToEntity(NpgsqlDataReader reader)
    {
        var statusStr = reader.GetString(reader.GetOrdinal(ColStatus));

        var status = Enum.Parse<DomainTaskStatus>(statusStr);

        var dueDateOrdinal = reader.GetOrdinal(ColDueDate);

        DateTime? dueDate = reader.IsDBNull(dueDateOrdinal) ? null : reader.GetDateTime(dueDateOrdinal);

        var descriptionOrdinal = reader.GetOrdinal(ColDescription);

        string? description = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal);

        return Domain.Entities.TaskFactory.Reconstitute(
            reader.GetGuid(reader.GetOrdinal(ColId)),
            reader.GetString(reader.GetOrdinal(ColTitle)),
            description,
            status,
            dueDate,
            reader.GetGuid(reader.GetOrdinal(ColUserId)),
            reader.GetDateTime(reader.GetOrdinal(ColCreatedAt)),
            reader.GetDateTime(reader.GetOrdinal(ColUpdatedAt)));
    }
}
