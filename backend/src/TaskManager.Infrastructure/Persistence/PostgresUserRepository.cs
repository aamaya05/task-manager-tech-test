using Npgsql;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;

namespace TaskManager.Infrastructure.Persistence;

public class PostgresUserRepository : IUserRepository
{
    private const string ColId = "id";
    private const string ColUsername = "username";
    private const string ColEmail = "email";
    private const string ColPasswordHash = "password_hash";
    private const string ColCreatedAt = "created_at";

    private readonly DbConnectionFactory _connectionFactory;

    public PostgresUserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"SELECT {ColId},{ColUsername},{ColEmail},{ColPasswordHash},{ColCreatedAt} FROM users WHERE {ColId} = @id",
            conn);

        cmd.Parameters.AddWithValue("@id", id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct)) return null;

        return MapToEntity(reader);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"SELECT {ColId},{ColUsername},{ColEmail},{ColPasswordHash},{ColCreatedAt} FROM users WHERE {ColEmail} = @email",
            conn);

        cmd.Parameters.AddWithValue("@email", email.ToLowerInvariant());

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct)) return null;

        return MapToEntity(reader);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"SELECT COUNT(1) FROM users WHERE {ColEmail} = @email", conn);

        cmd.Parameters.AddWithValue("@email", email.ToLowerInvariant());

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));

        return count > 0;
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await using var conn = _connectionFactory.CreateConnection();

        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(
            $"INSERT INTO users ({ColId},{ColUsername},{ColEmail},{ColPasswordHash},{ColCreatedAt}) " +
            $"VALUES (@id,@username,@email,@passwordHash,@createdAt) " +
            $"RETURNING {ColId},{ColUsername},{ColEmail},{ColPasswordHash},{ColCreatedAt}",
            conn);

        cmd.Parameters.AddWithValue("@id", user.Id);
        cmd.Parameters.AddWithValue("@username", user.Username);
        cmd.Parameters.AddWithValue("@email", user.Email.Value);
        cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@createdAt", user.CreatedAt);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        await reader.ReadAsync(ct);
        
        return MapToEntity(reader);
    }

    private static User MapToEntity(NpgsqlDataReader reader)
    {
        return UserFactory.Reconstitute(
            reader.GetGuid(reader.GetOrdinal(ColId)),
            reader.GetString(reader.GetOrdinal(ColUsername)),
            new Email(reader.GetString(reader.GetOrdinal(ColEmail))),
            reader.GetString(reader.GetOrdinal(ColPasswordHash)),
            reader.GetDateTime(reader.GetOrdinal(ColCreatedAt)));
    }
}
