using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class TaskItem : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AppTaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        private TaskItem() { }

        public static TaskItem Create(string title, string description, TaskPriority priority, Guid userId)
        {
            return new TaskItem()
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Title = title,
                Description = description,
                Status = AppTaskStatus.Pending,
                Priority = priority,
                UserId = userId
            };
        }
        public void UpdateStatus(AppTaskStatus newStatus) => Status = newStatus;
    }
}
