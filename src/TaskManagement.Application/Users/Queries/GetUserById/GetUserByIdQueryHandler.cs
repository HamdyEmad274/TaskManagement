using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserResponse>>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                return Result.Failure<UserResponse>("User not found");
            }
            var userDto = new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
            return Result.Success(userDto);
        }
    }
}
