namespace TaskManagement.Application.Users.DTOs;

public record RegisterUserRequest(string UserName, string Email, string Password);