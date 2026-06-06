using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Tests.Fixtures;

namespace TaskManager.Infrastructure.Tests.Repositories;

[Collection("PostgresIntegration")]
public class PostgresUserRepositoryTests
{
    private readonly PostgresUserRepository _repo;

    public PostgresUserRepositoryTests(PostgresContainerFixture fixture)
    {
        _repo = new PostgresUserRepository(fixture.ConnectionFactory);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresUserRepository_AddAsync_InsertsUserAndReturnsWithId()
    {
        var user = User.Create($"alice_{Guid.NewGuid():N}", new Email($"alice_{Guid.NewGuid():N}@example.com"), "$2b$12$testhash");

        var returned = await _repo.AddAsync(user);

        returned.Id.Should().NotBe(Guid.Empty);
        returned.Username.Should().Be(user.Username);
        returned.Email.Value.Should().Be(user.Email.Value);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresUserRepository_GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        var email = $"bob_{Guid.NewGuid():N}@example.com";
        await _repo.AddAsync(User.Create($"bob_{Guid.NewGuid():N}", new Email(email), "hash"));

        var result = await _repo.GetByEmailAsync(email);

        result.Should().NotBeNull();
        result!.Email.Value.Should().Be(email);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresUserRepository_GetByEmailAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var result = await _repo.GetByEmailAsync("nobody@example.com");

        result.Should().BeNull();
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresUserRepository_EmailExistsAsync_WhenEmailRegistered_ReturnsTrue()
    {
        var email = $"exists_{Guid.NewGuid():N}@example.com";
        await _repo.AddAsync(User.Create($"u_{Guid.NewGuid():N}", new Email(email), "hash"));

        var result = await _repo.EmailExistsAsync(email);

        result.Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresUserRepository_EmailExistsAsync_WhenEmailNotRegistered_ReturnsFalse()
    {
        var result = await _repo.EmailExistsAsync("notregistered@example.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresUserRepository_AddAsync_WithDuplicateEmail_ThrowsException()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        await _repo.AddAsync(User.Create($"first_{Guid.NewGuid():N}", new Email(email), "hash"));

        Func<System.Threading.Tasks.Task> act = () => _repo.AddAsync(User.Create($"second_{Guid.NewGuid():N}", new Email(email), "hash"));

        await act.Should().ThrowAsync<Exception>();
    }
}
