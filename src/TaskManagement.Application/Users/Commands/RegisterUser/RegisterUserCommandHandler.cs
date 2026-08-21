using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Users.Commands.RegisterUser
{
    public class RegisterUserCommandHandler
        : IRequestHandler<RegisterUserCommand, Result<UserResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterUserCommandHandler(IPasswordHasher passwordHasher,
            IUserRepository userRepository,
            IJwtTokenGenerator jwtTokenGenerator,
            IUnitOfWork unitOfWork)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _unitOfWork = unitOfWork;
        }
        public async Task<Result<UserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result.Failure<UserResponse>($"User with email {request.Email} already exists.");
            }
            var hashedPassword = _passwordHasher.Hash(request.Password);
            var user = User.Create(request.Name, request.Email, hashedPassword, Domain.Enums.UserRole.User);
            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var response = new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
            return Result.Success(response);
        }
    }
}
