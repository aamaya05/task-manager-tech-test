using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Interfaces;
using TaskManager.Domain.ValueObjects;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;
using DomainTask = TaskManager.Domain.Entities.Task;

namespace TaskManager.WebApi.Controllers;

[ApiController]
[Route("api/demo")]
[Authorize]
public class DemoController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IPasswordHasher _passwordHasher;

    private static readonly Guid DemoUserId = new("a1b2c3d4-0001-0001-0001-000000000001");
    private static readonly Guid AdminUserId = new("a1b2c3d4-0002-0002-0002-000000000002");

    public DemoController(
        IUserRepository userRepository,
        ITaskRepository taskRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _taskRepository = taskRepository;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("seed")]
    [ProducesResponseType(typeof(DemoSeedResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        var seeded = new List<string>();

        var isDemoEmailRegister = await _userRepository.EmailExistsAsync("demo@taskmanager.io", ct);

        if (!isDemoEmailRegister)
        {
            var demoUser = UserFactory.Reconstitute(
                DemoUserId,
                "demo_user",
                new Email("demo@taskmanager.io"),
                _passwordHasher.Hash("Demo1234!"),
                DateTime.UtcNow);

            await _userRepository.AddAsync(demoUser, ct);

            seeded.Add("demo_user (demo@taskmanager.io / Demo1234!)");

            await SeedTasks(DemoUserId, ct);

            seeded.Add("5 sample tasks for demo_user");
        }

        var isAdminEmailRegister = await _userRepository.EmailExistsAsync("admin@taskmanager.io", ct);

        if (!isAdminEmailRegister)
        {
            var adminUser = UserFactory.Reconstitute(
                AdminUserId,
                "admin",
                new Email("admin@taskmanager.io"),
                _passwordHasher.Hash("Admin1234!"),
                DateTime.UtcNow);

            await _userRepository.AddAsync(adminUser, ct);

            seeded.Add("admin (admin@taskmanager.io / Admin1234!)");

            await SeedTasks(AdminUserId, ct);

            seeded.Add("3 sample tasks for admin");
        }

        if (seeded.Count == 0)
        {
            return Ok(new DemoSeedResult(
                "Demo users already exist. No changes made.",
                [],
                [
                    new DemoCredential("demo_user", "demo@taskmanager.io", "Demo1234!"),
                    new DemoCredential("admin", "admin@taskmanager.io", "Admin1234!")
                ]));
        }

        return Ok(new DemoSeedResult(
            "Demo data seeded successfully.",
            seeded.ToArray(),
            [
                new DemoCredential("demo_user", "demo@taskmanager.io", "Demo1234!"),
                new DemoCredential("admin", "admin@taskmanager.io", "Admin1234!")
            ]));
    }

    private async System.Threading.Tasks.Task SeedTasks(Guid userId, CancellationToken ct)
    {
        var tasks = new[]
        {
            DomainTask.Create("Review pull requests",
                "Review and merge open PRs from the team",
                DomainTaskStatus.Todo, DateTime.UtcNow.AddDays(1), userId),
            DomainTask.Create("Update documentation",
                "Keep API docs and README up to date",
                DomainTaskStatus.InProgress, DateTime.UtcNow.AddDays(3), userId),
            DomainTask.Create("Deploy to staging",
                "Run the full deployment pipeline to staging environment",
                DomainTaskStatus.Todo, DateTime.UtcNow.AddDays(5), userId),
            DomainTask.Create("Implement domain layer",
                "Create entities, value objects, and repository interfaces",
                DomainTaskStatus.Done, DateTime.UtcNow.AddDays(-3), userId)
        };

        List<Task<DomainTask>> seedTasks = [];

        foreach (var task in tasks)
        {
            seedTasks.Add(_taskRepository.AddAsync(task, ct));
        }

        await System.Threading.Tasks.Task.WhenAll(seedTasks);
    }
}

public record DemoSeedResult(string Message, string[] Seeded, DemoCredential[] Credentials);
public record DemoCredential(string Username, string Email, string Password);
