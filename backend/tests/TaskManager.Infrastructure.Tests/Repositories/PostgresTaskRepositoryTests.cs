using FluentAssertions;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Tests.Fixtures;
using DomainTask = TaskManager.Domain.Entities.Task;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.Infrastructure.Tests.Repositories;

[Collection("PostgresIntegration")]
public class PostgresTaskRepositoryTests
{
    private readonly PostgresTaskRepository _repo;
    private readonly PostgresUserRepository _userRepo;
    private readonly Guid _userId;
    private readonly Guid _otherUserId;

    public PostgresTaskRepositoryTests(PostgresContainerFixture fixture)
    {
        _repo = new PostgresTaskRepository(fixture.ConnectionFactory);
        _userRepo = new PostgresUserRepository(fixture.ConnectionFactory);
        _userId = Guid.NewGuid();
        _otherUserId = Guid.NewGuid();

        System.Threading.Tasks.Task.Run(async () =>
        {
            await _userRepo.AddAsync(UserFactory.Reconstitute(_userId, $"user_{_userId:N}",
                new Email($"user_{_userId:N}@test.com"), "hash", DateTime.UtcNow));
            await _userRepo.AddAsync(UserFactory.Reconstitute(_otherUserId, $"user_{_otherUserId:N}",
                new Email($"user_{_otherUserId:N}@test.com"), "hash", DateTime.UtcNow));
        }).GetAwaiter().GetResult();
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_GetPagedByUserId_WithPage1PageSize2_ReturnsCorrectItemsAndTotalCount()
    {
        for (var i = 1; i <= 5; i++)
            await _repo.AddAsync(DomainTask.Create($"Task {i}", null, DomainTaskStatus.Todo, null, _userId));
        await _repo.AddAsync(DomainTask.Create("Other", null, DomainTaskStatus.Todo, null, _otherUserId));

        var (items, totalCount) = await _repo.GetPagedByUserIdAsync(_userId, page: 1, pageSize: 2);

        items.Should().HaveCount(2);
        totalCount.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_GetPagedByUserId_WithPage2PageSize2_ReturnsOffsetItems()
    {
        var uid = Guid.NewGuid();
        await _userRepo.AddAsync(UserFactory.Reconstitute(uid, $"u_{uid:N}",
            new Email($"u_{uid:N}@test.com"), "hash", DateTime.UtcNow));

        for (var i = 1; i <= 5; i++)
            await _repo.AddAsync(DomainTask.Create($"Paged {i}", null, DomainTaskStatus.Todo, null, uid));

        var (items, totalCount) = await _repo.GetPagedByUserIdAsync(uid, page: 2, pageSize: 2);

        items.Should().HaveCount(2);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_GetPagedByUserId_BeyondLastPage_ReturnsEmptyItemsWithCorrectTotalCount()
    {
        var uid = Guid.NewGuid();
        await _userRepo.AddAsync(UserFactory.Reconstitute(uid, $"u_{uid:N}",
            new Email($"u_{uid:N}@test.com"), "hash", DateTime.UtcNow));

        for (var i = 1; i <= 3; i++)
            await _repo.AddAsync(DomainTask.Create($"Task {i}", null, DomainTaskStatus.Todo, null, uid));

        var (items, totalCount) = await _repo.GetPagedByUserIdAsync(uid, page: 99, pageSize: 10);

        items.Should().BeEmpty();
        totalCount.Should().Be(3);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_GetPagedByUserId_ReturnsOnlyRequestingUserTasks()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await _userRepo.AddAsync(UserFactory.Reconstitute(userA, $"ua_{userA:N}",
            new Email($"ua_{userA:N}@test.com"), "hash", DateTime.UtcNow));
        await _userRepo.AddAsync(UserFactory.Reconstitute(userB, $"ub_{userB:N}",
            new Email($"ub_{userB:N}@test.com"), "hash", DateTime.UtcNow));

        for (var i = 1; i <= 3; i++)
            await _repo.AddAsync(DomainTask.Create($"A Task {i}", null, DomainTaskStatus.Todo, null, userA));
        for (var i = 1; i <= 2; i++)
            await _repo.AddAsync(DomainTask.Create($"B Task {i}", null, DomainTaskStatus.Todo, null, userB));

        var (itemsA, totalA) = await _repo.GetPagedByUserIdAsync(userA, 1, 10);
        var (itemsB, totalB) = await _repo.GetPagedByUserIdAsync(userB, 1, 10);

        totalA.Should().Be(3);
        totalB.Should().Be(2);
        itemsA.Should().AllSatisfy(t => t.UserId.Should().Be(userA));
        itemsB.Should().AllSatisfy(t => t.UserId.Should().Be(userB));
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_AddAsync_InsertsRowAndReturnsTaskWithCorrectFields()
    {
        var task = DomainTask.Create("My Task", "Description", DomainTaskStatus.Todo, DateTime.UtcNow.AddDays(5), _userId);

        var returned = await _repo.AddAsync(task);

        returned.Id.Should().NotBe(Guid.Empty);
        returned.Title.Should().Be("My Task");
        returned.Description.Should().Be("Description");
        returned.Status.Should().Be(DomainTaskStatus.Todo);
        returned.UserId.Should().Be(_userId);

        var fromDb = await _repo.GetByIdAsync(returned.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Title.Should().Be("My Task");
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_GetByIdAsync_WhenTaskExists_ReturnsCorrectTask()
    {
        var inserted = await _repo.AddAsync(DomainTask.Create("Find me", null, DomainTaskStatus.InProgress, null, _userId));

        var result = await _repo.GetByIdAsync(inserted.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Find me");
        result.Status.Should().Be(DomainTaskStatus.InProgress);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_GetByIdAsync_WhenTaskDoesNotExist_ReturnsNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_UpdateAsync_PersistsChangesToDatabase()
    {
        var task = await _repo.AddAsync(DomainTask.Create("Original", "Desc", DomainTaskStatus.Todo, null, _userId));
        Thread.Sleep(10);
        task.Update("Updated Title", "New Desc", DomainTaskStatus.InProgress, DateTime.UtcNow.AddDays(1));

        await _repo.UpdateAsync(task);
        var fromDb = await _repo.GetByIdAsync(task.Id);

        fromDb!.Title.Should().Be("Updated Title");
        fromDb.Status.Should().Be(DomainTaskStatus.InProgress);
        fromDb.UpdatedAt.Should().BeAfter(fromDb.CreatedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_DeleteAsync_RemovesRowFromDatabase()
    {
        var task = await _repo.AddAsync(DomainTask.Create("To Delete", null, DomainTaskStatus.Todo, null, _userId));

        await _repo.DeleteAsync(task.Id);

        var fromDb = await _repo.GetByIdAsync(task.Id);
        fromDb.Should().BeNull();
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_ExistsAsync_WhenTaskExists_ReturnsTrue()
    {
        var task = await _repo.AddAsync(DomainTask.Create("Exists", null, DomainTaskStatus.Todo, null, _userId));

        var result = await _repo.ExistsAsync(task.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async System.Threading.Tasks.Task PostgresTaskRepository_ExistsAsync_WhenTaskDoesNotExist_ReturnsFalse()
    {
        var result = await _repo.ExistsAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}
