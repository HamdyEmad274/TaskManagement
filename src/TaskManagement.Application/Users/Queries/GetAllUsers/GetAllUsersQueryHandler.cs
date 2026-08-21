using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Users.Queries.GetAllUsers
{
    public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<IEnumerable<UserResponse>>>
    {
        private readonly IUserRepository _userRepository;

        public GetAllUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<IEnumerable<UserResponse>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = new List<UserResponse>();
            foreach (var user in users)
            {
                userDtos.Add(new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt));
            }
            return Result.Success<IEnumerable<UserResponse>>(userDtos);
        }
    }
}
