namespace TaskManager.Application.Users.Commands;

public record RegisterUserCommand(string Username, string Email, string Password);
