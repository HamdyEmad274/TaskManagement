using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Users.Interfaces;

public interface IUserService
{
    Task<Result<UserResponse>> RegisterAsync(RegisterUserRequest request , CancellationToken cancellationToken = default);
    Task<Result<string>> LoginAsync(LoginRequest request);
    Task<Result<UserResponse>> GetByIdAsync(Guid id);
    Task<Result<IEnumerable<UserResponse>>> GetAllAsync();
    Task<Result> DeleteAsync(Guid id);
}