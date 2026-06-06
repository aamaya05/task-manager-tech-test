using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;
using TaskManager.Infrastructure.Persistence;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.WebApi.Tests.Fixtures;

public class TaskManagerWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestJwtSecret = "test-secret-key-that-is-long-enough-for-hs256-signing";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("taskmanager_webapi_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        await _container.StartAsync();
        await RunMigrationsAsync();
    }

    public new async System.Threading.Tasks.Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
                ["Jwt:Secret"] = TestJwtSecret,
                ["Jwt:Issuer"] = "taskmanager-api",
                ["Jwt:Audience"] = "taskmanager-client",
                ["Jwt:ExpiryHours"] = "1"
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var token = GenerateTestJwt(userId, DateTime.UtcNow.AddHours(1));
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateClientWithExpiredJwt()
    {
        var token = GenerateTestJwt(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1));
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<User> SeedUserAsync(Guid? id = null, string? email = null)
    {
        using var scope = Services.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var userId = id ?? Guid.NewGuid();
        var userEmail = email ?? $"user_{userId:N}@test.com";
        var user = UserFactory.Reconstitute(userId, $"user_{userId:N}",
            new Email(userEmail), "$2b$12$testhash", DateTime.UtcNow);
        return await userRepo.AddAsync(user);
    }

    public async Task<DomainTask> SeedTaskAsync(Guid userId, string title = "Test Task")
    {
        using var scope = Services.CreateScope();
        var taskRepo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var task = DomainTask.Create(title, null, DomainTaskStatus.Todo, null, userId);
        return await taskRepo.AddAsync(task);
    }

    private string GenerateTestJwt(Guid userId, DateTime expiry)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: "taskmanager-api",
            audience: "taskmanager-client",
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async System.Threading.Tasks.Task RunMigrationsAsync()
    {
        var factory = new DbConnectionFactory(_container.GetConnectionString());
        var migrationsPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "TaskManager.Infrastructure", "Migrations");

        var scripts = Directory.GetFiles(migrationsPath, "*.sql")
            .OrderBy(f => f)
            .ToArray();

        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();

        foreach (var script in scripts)
        {
            var sql = await File.ReadAllTextAsync(script);
            await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
