using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks.DTOs;

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
}