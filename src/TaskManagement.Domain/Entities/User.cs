using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }
        private User() { }
        public static User Create(string name, string email, string passwordHash, UserRole role)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Name = name,
                Email = email,
                PasswordHash = passwordHash,
                Role = role
            };
        }
    }
}
