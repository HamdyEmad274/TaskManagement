using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Tasks.Interfaces;

public interface ITaskService
{
    Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, Guid userId , CancellationToken cancellationToken = default);
    Task<Result<TaskResponse>> GetByIdAsync(Guid id, Guid userId);
    Task<Result<IEnumerable<TaskResponse>>> GetAllByUserAsync(Guid userId);
    Task<Result<TaskResponse>> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, Guid userId , CancellationToken cancellationToken = default);
}