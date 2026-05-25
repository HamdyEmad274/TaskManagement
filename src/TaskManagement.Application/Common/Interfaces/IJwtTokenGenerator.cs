using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}