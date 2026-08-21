using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Users.Commands.DeleteUser
{
    public record DeleteUserCommand(Guid Id) :
        IRequest<Result>;
}
