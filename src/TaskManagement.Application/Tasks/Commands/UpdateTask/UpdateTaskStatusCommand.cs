using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks.Commands.UpdateTask
{
    public record UpdateTaskStatusCommand(Guid Id, AppTaskStatus Status, Guid UserId) :
        IRequest<Result<TaskResponse>>;
}
