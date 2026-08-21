using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Users.Commands.LoginUser
{
    public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<string>>
    {
        private const string InvalidCredentialsErrorMessage = "Email or password is incorrect. Please try again.";
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        public LoginUserCommandHandler(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<string>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Result.Failure<string>(InvalidCredentialsErrorMessage);
            }
            if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Result.Failure<string>(InvalidCredentialsErrorMessage);
            }
            var token = _jwtTokenGenerator.GenerateToken(user);
            return Result.Success(token);
        }
    }
}
