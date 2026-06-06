using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Domain.Exceptions;

namespace TaskManager.WebApi.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, type, title, detail) = exception switch
        {
            TaskNotFoundException e => (404, "not_found", "Resource Not Found", e.Message),
            UnauthorizedTaskAccessException e => (403, "forbidden", "Forbidden", e.Message),
            DuplicateEmailException e => (409, "duplicate_email", "Conflict", e.Message),
            InvalidEmailException e => (400, "validation_error", "Validation Error", e.Message),
            DomainException e => (400, "validation_error", "Validation Error", e.Message),
            _ => (500, "internal_error", "Internal Server Error", "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        
        context.Response.ContentType = "application/problem+json";

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }
}
