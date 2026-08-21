using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Tasks.Commands.DeleteTask
{
    public record DeleteTaskCommand(Guid Id, Guid UserId) :
        IRequest<Result>;
}
