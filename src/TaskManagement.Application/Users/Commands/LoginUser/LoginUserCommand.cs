using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Users.Commands.LoginUser
{
    public record LoginUserCommand(string Email, string Password)
        : IRequest<Result<string>>;
}
