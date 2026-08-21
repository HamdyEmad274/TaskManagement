using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Tasks.Commands.CreateTask
{
    public record CreateTaskCommand(string Title, string Description, TaskPriority Priority, Guid UserId) :
        IRequest<Result<TaskResponse>>;
}
