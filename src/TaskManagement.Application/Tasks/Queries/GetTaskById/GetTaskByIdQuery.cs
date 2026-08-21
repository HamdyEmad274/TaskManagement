using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Tasks.Queries.GetTaskById
{
    public record GetTaskByIdQuery(Guid Id, Guid UserId)
        : IRequest<Result<TaskResponse>>;
}
