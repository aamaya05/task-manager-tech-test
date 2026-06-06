using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TaskManager.WebApi.Tests.Fixtures;

namespace TaskManager.WebApi.Tests.Controllers;

public class TasksControllerTests : IClassFixture<TaskManagerWebApplicationFactory>
{
    private readonly TaskManagerWebApplicationFactory _factory;
    private static readonly JsonDocumentOptions JsonOptions = new() { AllowTrailingCommas = true };

    public TasksControllerTests(TaskManagerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Parses JSON only when the response has a non-empty body.
    // Fails the test with a clear message if the body is unexpectedly empty.
    private static JsonElement ParseJson(HttpResponseMessage response, string content)
    {
        content.Should().NotBeNullOrWhiteSpace(
            because: $"expected a JSON body but got an empty response (status {(int)response.StatusCode})");
        return JsonDocument.Parse(content, JsonOptions).RootElement;
    }

    // ── GET /api/tasks

    [Fact]
    public async Task TasksController_GetAll_WithDefaultPageParams_Returns200PagedResult()
    {
        var user = await _factory.SeedUserAsync();
        for (var i = 1; i <= 15; i++)
            await _factory.SeedTaskAsync(user.Id, $"Task {i}");
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/tasks");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.GetProperty("items").GetArrayLength().Should().Be(10);
        json.GetProperty("totalCount").GetInt32().Should().Be(15);
        json.GetProperty("page").GetInt32().Should().Be(1);
        json.GetProperty("pageSize").GetInt32().Should().Be(10);
        json.GetProperty("totalPages").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task TasksController_GetAll_WithExplicitPageAndPageSize_Returns200CorrectSlice()
    {
        var user = await _factory.SeedUserAsync();
        for (var i = 1; i <= 7; i++)
            await _factory.SeedTaskAsync(user.Id, $"Slice Task {i}");
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/tasks?page=2&pageSize=3");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.GetProperty("items").GetArrayLength().Should().Be(3);
        json.GetProperty("totalCount").GetInt32().Should().Be(7);
        json.GetProperty("page").GetInt32().Should().Be(2);
        json.GetProperty("pageSize").GetInt32().Should().Be(3);
        json.GetProperty("totalPages").GetInt32().Should().Be(3);
    }

    [Fact]
    public async Task TasksController_GetAll_BeyondLastPage_Returns200WithEmptyItems()
    {
        var user = await _factory.SeedUserAsync();
        for (var i = 1; i <= 3; i++)
            await _factory.SeedTaskAsync(user.Id, $"Beyond Task {i}");
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/tasks?page=99&pageSize=10");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.GetProperty("items").GetArrayLength().Should().Be(0);
        json.GetProperty("totalCount").GetInt32().Should().Be(3);
        json.GetProperty("totalPages").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task TasksController_GetAll_OnlyReturnsAuthenticatedUserTasks()
    {
        var userA = await _factory.SeedUserAsync();
        var userB = await _factory.SeedUserAsync();
        for (var i = 1; i <= 3; i++) await _factory.SeedTaskAsync(userA.Id, $"A {i}");
        for (var i = 1; i <= 2; i++) await _factory.SeedTaskAsync(userB.Id, $"B {i}");
        var client = _factory.CreateAuthenticatedClient(userA.Id);

        var response = await client.GetAsync("/api/tasks");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.GetProperty("totalCount").GetInt32().Should().Be(3);
        foreach (var item in json.GetProperty("items").EnumerateArray())
            item.GetProperty("userId").GetString().Should().Be(userA.Id.ToString());
    }

    [Fact]
    public async Task TasksController_GetAll_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tasks");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TasksController_GetAll_WithExpiredToken_Returns401()
    {
        var client = _factory.CreateClientWithExpiredJwt();

        var response = await client.GetAsync("/api/tasks?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/tasks/{id}

    [Fact]
    public async Task TasksController_GetById_WhenTaskBelongsToUser_Returns200()
    {
        var user = await _factory.SeedUserAsync();
        var task = await _factory.SeedTaskAsync(user.Id, "My Task");
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/tasks/{task.Id}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.GetProperty("id").GetString().Should().Be(task.Id.ToString());
        json.GetProperty("title").GetString().Should().Be("My Task");
    }

    [Fact]
    public async Task TasksController_GetById_WhenTaskBelongsToDifferentUser_Returns403()
    {
        var owner = await _factory.SeedUserAsync();
        var other = await _factory.SeedUserAsync();
        var task = await _factory.SeedTaskAsync(owner.Id);
        var client = _factory.CreateAuthenticatedClient(other.Id);

        var response = await client.GetAsync($"/api/tasks/{task.Id}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/problem+json");
        var json = ParseJson(response, content);
        json.GetProperty("status").GetInt32().Should().Be(403);
    }

    [Fact]
    public async Task TasksController_GetById_WhenTaskDoesNotExist_Returns404ProblemDetails()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/problem+json");
        var json = ParseJson(response, content);
        json.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task TasksController_GetById_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/tasks

    [Fact]
    public async Task TasksController_Create_WithValidBody_Returns201WithLocationHeader()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);
        var body = new { title = "New Task", description = "Details", status = "Todo", dueDate = "2025-03-01T00:00:00Z" };

        var response = await client.PostAsJsonAsync("/api/tasks", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var json = ParseJson(response, content);
        json.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("title").GetString().Should().Be("New Task");
        json.GetProperty("userId").GetString().Should().Be(user.Id.ToString());
    }

    [Fact]
    public async Task TasksController_Create_WithMissingTitle_Returns400()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);
        var body = new { description = "No title" };

        var response = await client.PostAsJsonAsync("/api/tasks", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.ToLower().Should().Contain("title");
    }

    [Fact]
    public async Task TasksController_Create_WithInvalidStatus_Returns400()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);
        var body = new { title = "Task", status = "NOT_VALID" };

        var response = await client.PostAsJsonAsync("/api/tasks", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TasksController_Create_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "Task" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── PUT /api/tasks/{id}

    [Fact]
    public async Task TasksController_Update_WithValidBody_Returns200UpdatedTask()
    {
        var user = await _factory.SeedUserAsync();
        var task = await _factory.SeedTaskAsync(user.Id, "Original");
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.PutAsJsonAsync($"/api/tasks/{task.Id}",
            new { title = "Updated", status = "Done" });
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.GetProperty("title").GetString().Should().Be("Updated");
        json.GetProperty("status").GetString().Should().Be("Done");
    }

    [Fact]
    public async Task TasksController_Update_WhenTaskBelongsToDifferentUser_Returns403()
    {
        var owner = await _factory.SeedUserAsync();
        var other = await _factory.SeedUserAsync();
        var task = await _factory.SeedTaskAsync(owner.Id);
        var client = _factory.CreateAuthenticatedClient(other.Id);

        var response = await client.PutAsJsonAsync($"/api/tasks/{task.Id}", new { title = "Hacked" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TasksController_Update_WhenTaskDoesNotExist_Returns404()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", new { title = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/tasks/{id}

    [Fact]
    public async Task TasksController_Delete_WhenTaskBelongsToUser_Returns204AndTaskIsGone()
    {
        var user = await _factory.SeedUserAsync();
        var task = await _factory.SeedTaskAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var deleteResponse = await client.DeleteAsync($"/api/tasks/{task.Id}");
        var getResponse = await client.GetAsync($"/api/tasks/{task.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TasksController_Delete_WhenTaskBelongsToDifferentUser_Returns403()
    {
        var owner = await _factory.SeedUserAsync();
        var other = await _factory.SeedUserAsync();
        var task = await _factory.SeedTaskAsync(owner.Id);
        var client = _factory.CreateAuthenticatedClient(other.Id);

        var response = await client.DeleteAsync($"/api/tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TasksController_Delete_WhenTaskDoesNotExist_Returns404()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TasksController_Delete_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Error contract

    [Fact]
    public async Task ErrorContract_404Response_HasCorrectProblemDetailsShape()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = ParseJson(response, content);
        json.GetProperty("type").GetString().Should().NotBeNull();
        json.GetProperty("title").GetString().Should().NotBeNull();
        json.GetProperty("status").GetInt32().Should().Be(404);
        json.GetProperty("detail").GetString().Should().NotBeNull();
    }

    [Fact]
    public async Task ErrorContract_PagedResult_AlwaysHasAllPaginationFields()
    {
        var user = await _factory.SeedUserAsync();
        for (var i = 1; i <= 5; i++)
            await _factory.SeedTaskAsync(user.Id, $"Pagination Task {i}");
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/tasks?page=1&pageSize=3");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.TryGetProperty("items", out _).Should().BeTrue();
        json.TryGetProperty("totalCount", out _).Should().BeTrue();
        json.TryGetProperty("page", out _).Should().BeTrue();
        json.TryGetProperty("pageSize", out _).Should().BeTrue();
        json.TryGetProperty("totalPages", out _).Should().BeTrue();
        json.GetProperty("totalPages").GetInt32().Should().Be(2);
    }
}
