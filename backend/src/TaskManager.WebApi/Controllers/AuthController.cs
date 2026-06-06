using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Users.Commands;
using TaskManager.Application.Users.Handlers;
using TaskManager.Application.Validators;
using TaskManager.Domain.Interfaces;

namespace TaskManager.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserHandler _registerHandler;
    private readonly LoginUserHandler _loginHandler;
    private readonly IUserRepository _userRepository;
    private readonly RegisterUserCommandValidator _registerValidator;

    public AuthController(
        RegisterUserHandler registerHandler,
        LoginUserHandler loginHandler,
        IUserRepository userRepository,
        RegisterUserCommandValidator registerValidator)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _userRepository = userRepository;
        _registerValidator = registerValidator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken ct)
    {
        var command = new RegisterUserCommand(request.Username, request.Email, request.Password);

        var validation = await _registerValidator.ValidateAsync(command, ct);

        if (!validation.IsValid)
        {
            return ValidationProblem(validation);
        }
        
        var result = await _registerHandler.Handle(command, ct);

        return CreatedAtAction(nameof(Me), new { }, result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Problem(
                type: "validation_error",
                title: "Validation Error",
                detail: "Email and password are required.",
                statusCode: 400);
        }

        var command = new LoginUserCommand(request.Email, request.Password);

        var result = await _loginHandler.Handle(command, ct);

        if (result is null)
        {
            return Problem(
                type: "invalid_credentials",
                title: "Unauthorized",
                detail: "Invalid credentials.",
                statusCode: 401);
        }

        return Ok(result);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId)) return Unauthorized();

        var user = await _userRepository.GetByIdAsync(userId, ct);

        if (user is null) return Unauthorized();

        return Ok(new UserDto(user.Id, user.Username, user.Email.Value, user.CreatedAt));
    }

    private IActionResult ValidationProblem(FluentValidation.Results.ValidationResult validation)
    {
        var errors = validation.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return ValidationProblem(new ValidationProblemDetails(errors)
        {
            Type = "validation_error",
            Title = "Validation Error",
            Status = 400
        });
    }
}

public record RegisterUserRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);
