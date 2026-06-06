using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TaskManager.WebApi.Tests.Fixtures;

namespace TaskManager.WebApi.Tests.Controllers;

public class AuthControllerTests : IClassFixture<TaskManagerWebApplicationFactory>
{
    private readonly TaskManagerWebApplicationFactory _factory;
    private static readonly JsonDocumentOptions JsonOptions = new() { AllowTrailingCommas = true };

    public AuthControllerTests(TaskManagerWebApplicationFactory factory)
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

    [Fact]
    public async Task AuthController_Register_WithValidBody_Returns201AndUserObjectWithoutPasswordHash()
    {
        var client = _factory.CreateClient();
        var body = new { username = "new_user_test", email = $"new_{Guid.NewGuid():N}@example.com", password = "SecurePass1!" };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = ParseJson(response, content);
        json.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("username").GetString().Should().Be("new_user_test");
        content.Should().NotContain("passwordHash");
        content.Should().NotContain("password_hash");
    }

    [Fact]
    public async Task AuthController_Register_WithDuplicateEmail_Returns409ProblemDetails()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        await _factory.SeedUserAsync(email: email);
        var client = _factory.CreateClient();
        var body = new { username = "other_user", email, password = "SecurePass1!" };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/problem+json");
        content.Should().Contain("duplicate_email");
    }

    [Fact]
    public async Task AuthController_Register_WithMissingEmailField_Returns400ProblemDetails()
    {
        var client = _factory.CreateClient();
        var body = new { username = "user", password = "SecurePass1!" };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("validation errors");
    }

    [Fact]
    public async Task AuthController_Register_WithInvalidEmailFormat_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { username = "user", email = "not-valid", password = "SecurePass1!" };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthController_Login_WithValidCredentials_Returns200AndJwtToken()
    {
        var email = $"login_{Guid.NewGuid():N}@example.com";
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new { username = $"loginuser_{Guid.NewGuid():N}", email, password = "SecurePass1!" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SecurePass1!" });
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        var token = json.GetProperty("token").GetString()!;
        token.Split('.').Should().HaveCount(3);
        json.GetProperty("userId").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("username").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthController_Login_WithWrongPassword_Returns401ProblemDetails()
    {
        var email = $"wrongpw_{Guid.NewGuid():N}@example.com";
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new { username = $"wpuser_{Guid.NewGuid():N}", email, password = "SecurePass1!" });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass!" });
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        content.Should().Contain("invalid_credentials");
    }

    [Fact]
    public async Task AuthController_Login_WithUnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@nowhere.com", password = "irrelevant" });

        // Status assertion only — login returns 401 with a body from our controller,
        // but we only verify the status code here per the spec.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthController_Health_WithNoToken_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/health");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task AuthController_Me_WithValidToken_Returns200AndUserInfo()
    {
        var user = await _factory.SeedUserAsync();
        var client = _factory.CreateAuthenticatedClient(user.Id);

        var response = await client.GetAsync("/api/auth/me");
        var content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = ParseJson(response, content);
        json.GetProperty("id").GetString().Should().Be(user.Id.ToString());
        json.GetProperty("username").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("email").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthController_Me_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        // Status assertion only — no body parsing needed for a bare 401 check
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
