using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Common;

namespace TaskManagement.Application.Users.Queries.GetUserById
{
    public record GetUserByIdQuery(Guid Id)
        : IRequest<Result<UserResponse>>;
}
