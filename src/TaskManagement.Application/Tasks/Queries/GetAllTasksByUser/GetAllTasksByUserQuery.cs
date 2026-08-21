using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Tasks.Queries.GetAllTasksByUser
{
    public record GetAllTasksByUserQuery(Guid UserId)
        : IRequest<Result<IEnumerable<TaskResponse>>>;
}
