using Testcontainers.PostgreSql;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Tests.Fixtures;

public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("taskmanager_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public DbConnectionFactory ConnectionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionFactory = new DbConnectionFactory(_container.GetConnectionString());
        await RunMigrationsAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task RunMigrationsAsync()
    {
        var migrationsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "TaskManager.Infrastructure", "Migrations");

        var scripts = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(f => f)
            .ToArray();

        await using var conn = ConnectionFactory.CreateConnection();
        await conn.OpenAsync();

        foreach (var script in scripts)
        {
            var sql = await File.ReadAllTextAsync(script);
            await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition("PostgresIntegration")]
public class PostgresIntegrationCollection : ICollectionFixture<PostgresContainerFixture> { }
