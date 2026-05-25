using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Users.DTOs;
using TaskManagement.Application.Users.Interfaces;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Users.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IPasswordHasher passwordHasher, IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IUnitOfWork unitOfWork)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _unitOfWork = unitOfWork;
        }

        public Task<Result> DeleteAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<UserResponse>>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Result<UserResponse>> GetByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<string>> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Result.Failure<string>($"email or password is incorrect. Please try again.");
            }
            var passwordIsValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
            if (!passwordIsValid)
            {
                return Result.Failure<string>($"email or password is incorrect. Please try again.");
            }
            var token = _jwtTokenGenerator.GenerateToken(user);
            return Result.Success(token);
        }

        public async Task<Result<UserResponse>> RegisterAsync(RegisterUserRequest request , CancellationToken cancellationToken = default)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return Result.Failure<UserResponse>($"User with email {request.Email} already exists.");
            }
            var hashedPassword = _passwordHasher.Hash(request.Password);
            var user = User.Create(request.UserName, request.Email, hashedPassword, Domain.Enums.UserRole.User);
            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var response = new UserResponse(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
            return Result.Success(response);
        }
    }
}
