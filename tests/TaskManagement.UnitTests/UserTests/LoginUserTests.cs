using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Users.Commands.LoginUser;
using TaskManagement.Application.Users.Commands.RegisterUser;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.UnitTests.UserTests
{
    public class LoginUserTests
    {
        [Fact]
        public async Task Handle_ValidRequest_LoginUserSuccessfully()
        {
            // Arrange
            var mockUserRepository = new Mock<IUserRepository>();
            var mockPasswordHasher = new Mock<IPasswordHasher>();
            var mockJwtGenerator = new Mock<IJwtTokenGenerator>();

            var user = User.Create("test", "test@test.com", "passwordHash", Domain.Enums.UserRole.User);
            mockUserRepository.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            mockPasswordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);


            var handler = new LoginUserCommandHandler(
                mockUserRepository.Object,
                mockJwtGenerator.Object,
                mockPasswordHasher.Object
                );

            // Act
            var result = await handler.Handle(new LoginUserCommand("test@test.com", "password"), new CancellationToken());
            // Assert
            result.IsSuccess.Should().BeTrue();

            mockUserRepository.Verify(repo => repo.GetByEmailAsync(It.IsAny<string>()), Times.Once);
            mockPasswordHasher.Verify(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            mockJwtGenerator.Verify(jwt => jwt
            .GenerateToken(It.IsAny<User>()), Times.Once);

        }
        [Fact]
        public async Task Handle_UserNotFound_ShouldFailAsync()
        {
            //Arrange
            var mockUserRepository = new Mock<IUserRepository>();
            var mockPasswordHasher = new Mock<IPasswordHasher>();
            var mockJwtGenerator = new Mock<IJwtTokenGenerator>();

            mockUserRepository.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

            var handler = new LoginUserCommandHandler(mockUserRepository.Object,
                mockJwtGenerator.Object, mockPasswordHasher.Object);

            var command = new LoginUserCommand("test@test.com", "password");

            //Act
            var result = await handler.Handle(command, new CancellationToken());

            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeEquivalentTo("Email or password is incorrect. Please try again.");

            mockPasswordHasher.Verify(hasher=> hasher.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            mockJwtGenerator.Verify(jwt => jwt.GenerateToken(It.IsAny<User>()), Times.Never);
        }
        [Fact]
        public async Task Handle_InvalidPassword_ShouldFailAsync()
        {
            //Arrange
            var mockUserRepository = new Mock<IUserRepository>();
            var mockPasswordHasher = new Mock<IPasswordHasher>();
            var mockJwtGenerator = new Mock<IJwtTokenGenerator>();

            var user = User.Create("test", "test@test.com", "passwordHash", Domain.Enums.UserRole.User);
            mockUserRepository.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            mockPasswordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

            var handler = new LoginUserCommandHandler(mockUserRepository.Object,
                mockJwtGenerator.Object, mockPasswordHasher.Object);

            var command = new LoginUserCommand("test@test.com", "password");

            //Act
            var result = await handler.Handle(command, new CancellationToken());

            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeEquivalentTo("Email or password is incorrect. Please try again.");

            mockUserRepository.Verify(repo => repo.GetByEmailAsync(It.IsAny<string>()), Times.Once);
            mockPasswordHasher.Verify(hasher=> hasher.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            mockJwtGenerator.Verify(jwt => jwt.GenerateToken(It.IsAny<User>()), Times.Never);
        }
    }
}
