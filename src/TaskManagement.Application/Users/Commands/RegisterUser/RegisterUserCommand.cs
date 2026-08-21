using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Users.Commands.RegisterUser
{
    public record RegisterUserCommand(string Name, string Email, string Password)
        : IRequest<Result<UserResponse>>;
}
