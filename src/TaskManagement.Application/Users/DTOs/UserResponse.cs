namespace TaskManagement.Application.Users.DTOs;


public record UserResponse(Guid Id, string Name, string Email , string Role, DateTime CreatedAt);