using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Users.Commands.DeleteUser;
using TaskManagement.Application.Users.Commands.LoginUser;
using TaskManagement.Application.Users.Commands.RegisterUser;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.UnitTests.UserTests
{
    public class DeleteUserTests
    {
        [Fact]
        public async Task Handle_ValidRequest_DeleteUserSuccessfully()
        {
            // Arrange
            var mockUserRepository = new Mock<IUserRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var user = User.Create("test", "test@test.com", "passwordHash", Domain.Enums.UserRole.User);
            mockUserRepository.Setup(repo => repo.GetByIdAsync(user.Id)).ReturnsAsync(user);
            
            var handler = new DeleteUserCommandHandler(mockUserRepository.Object, mockUnitOfWork.Object);
            var cmd = new DeleteUserCommand(user.Id);
            
            // Act
            var result = await handler.Handle(cmd, new CancellationToken());

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockUserRepository.Verify(repo => repo.DeleteAsync(user), Times.Once);
            mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);

        }
        [Fact]
        public async Task Handle_UserNotFound_ShouldFailAsync()
        {
            //Arrange
            var mockUserRepository = new Mock<IUserRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var falseUserId = Guid.NewGuid();
            mockUserRepository.Setup(repo => repo.GetByIdAsync(falseUserId)).ReturnsAsync((User?)null);

            var handler = new DeleteUserCommandHandler(mockUserRepository.Object, mockUnitOfWork.Object);
            var cmd = new DeleteUserCommand(falseUserId);

            // Act
            var result = await handler.Handle(cmd, new CancellationToken());

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("User does not exist");

            mockUserRepository.Verify(repo => repo.DeleteAsync(It.IsAny<User>()), Times.Never);
            mockUnitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
        }
        
    }
}
