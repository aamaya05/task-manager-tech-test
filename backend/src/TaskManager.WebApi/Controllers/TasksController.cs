using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.DTOs;
using TaskManager.Application.Tasks.Commands;
using TaskManager.Application.Tasks.Handlers;
using TaskManager.Application.Tasks.Queries;
using TaskManager.Application.Validators;
using DomainTaskStatus = TaskManager.Domain.ValueObjects.TaskStatus;

namespace TaskManager.WebApi.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly CreateTaskHandler _createHandler;
    private readonly UpdateTaskHandler _updateHandler;
    private readonly DeleteTaskHandler _deleteHandler;
    private readonly GetTaskByIdHandler _getByIdHandler;
    private readonly GetPagedTasksHandler _getPagedHandler;
    private readonly CreateTaskCommandValidator _createValidator;
    private readonly UpdateTaskCommandValidator _updateValidator;

    public TasksController(
        CreateTaskHandler createHandler,
        UpdateTaskHandler updateHandler,
        DeleteTaskHandler deleteHandler,
        GetTaskByIdHandler getByIdHandler,
        GetPagedTasksHandler getPagedHandler,
        CreateTaskCommandValidator createValidator,
        UpdateTaskCommandValidator updateValidator)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _getByIdHandler = getByIdHandler;
        _getPagedHandler = getPagedHandler;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        if (pageSize < 1) pageSize = 1;

        if (pageSize > 100) pageSize = 100;

        var userId = GetUserId();

        var query = new GetPagedTasksQuery(userId, page, pageSize);

        var result = await _getPagedHandler.Handle(query, ct);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();

        var result = await _getByIdHandler.Handle(new GetTaskByIdQuery(id, userId), ct);

        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var userId = GetUserId();

        var status = ParseStatus(request.Status);

        var command = new CreateTaskCommand(request.Title, request.Description, status, request.DueDate, userId);

        var validation = await _createValidator.ValidateAsync(command, ct);
        
        if (!validation.IsValid) return ValidationProblem(validation);

        var result = await _createHandler.Handle(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request, CancellationToken ct)
    {
        var userId = GetUserId();

        var status = ParseStatus(request.Status);

        var command = new UpdateTaskCommand(id, request.Title, request.Description, status, request.DueDate, userId);

        var validation = await _updateValidator.ValidateAsync(command, ct);

        if (!validation.IsValid) return ValidationProblem(validation);

        var result = await _updateHandler.Handle(command, ct);

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();

        await _deleteHandler.Handle(new DeleteTaskCommand(id, userId), ct);

        return NoContent();
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static DomainTaskStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return DomainTaskStatus.Todo;
        
        return Enum.TryParse<DomainTaskStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : throw new Domain.Exceptions.DomainException($"Invalid status value: '{status}'. Must be Todo, InProgress, or Done.");
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

public record CreateTaskRequest(string Title, string? Description, string? Status, DateTime? DueDate);
public record UpdateTaskRequest(string Title, string? Description, string? Status, DateTime? DueDate);
