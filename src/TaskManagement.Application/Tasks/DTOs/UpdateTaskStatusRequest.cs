using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks.DTOs;

public class UpdateTaskStatusRequest
{
    public AppTaskStatus Status { get; set; }
}