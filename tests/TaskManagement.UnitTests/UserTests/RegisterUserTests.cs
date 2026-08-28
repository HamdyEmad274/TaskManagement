using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Users.Commands.RegisterUser;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.UnitTests.UserTests
{
    public class RegisterUserTests
    {
        [Fact]
        public async Task Handle_ValidRequest_CreateUserSuccessfully()
        {
            // Arrange
            var mockUserRepository = new Mock<IUserRepository>();
            var mockPasswordHasher = new Mock<IPasswordHasher>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUserRepository.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);
            mockPasswordHasher.Setup(hasher => hasher.Hash(It.IsAny<string>()))
                .Returns("hashedPassword");
            mockUnitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var handler = new RegisterUserCommandHandler(mockPasswordHasher.Object,
                mockUserRepository.Object,
                mockUnitOfWork.Object);
            // Act
            var result = await handler.Handle(new RegisterUserCommand("test", "test@test.com", "password"), new CancellationToken());
            // Assert
            result.IsSuccess.Should().BeTrue();

            mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
            mockUnitOfWork.Verify(unitOfWork => unitOfWork
            .SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        }
        [Fact]
        public async Task Handle_DuplicateEmail_ShouldFailAsync()
        {
            //Arrange
            var mockUserRepository = new Mock<IUserRepository>();
            var mockPasswordHasher = new Mock<IPasswordHasher>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var existingUser = User.Create("Existing","test@test.com","passwordHash",Domain.Enums.UserRole.User);

            mockUserRepository.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

            var handler = new RegisterUserCommandHandler(mockPasswordHasher.Object, mockUserRepository.Object,
                mockUnitOfWork.Object);

            var command = new RegisterUserCommand("test", "test@test.com", "password");

            //Act
            var result = await handler.Handle(command, new CancellationToken());

            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("exists");

            mockPasswordHasher.Verify(hasher=> hasher.Hash(It.IsAny<string>()), Times.Never);
            mockUserRepository.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
            mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
