using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.Commands.DeleteTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.UnitTests.TasksTests
{
    public class DeleteTaskTests
    {
        [Fact]
        public async Task DeleteTask_TaskExists_UserIsOwner_ShouldDeleteTask()
        {
            //Arrange
            var mockTaskRepository = new Mock<ITaskRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var userId = Guid.NewGuid();
            var existingTask = TaskItem.Create("test","test", TaskPriority.High ,userId);

            mockTaskRepository.Setup(repo=>repo.GetByIdAsync(existingTask.Id)).ReturnsAsync(existingTask);

            var handler = new DeleteTaskCommandHandler(mockTaskRepository.Object, mockUnitOfWork.Object);
            var cmd = new DeleteTaskCommand(existingTask.Id , userId);
            //Act
            var result = await handler.Handle(cmd, CancellationToken.None);
            //Assert
            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeEmpty();
            mockTaskRepository.Verify(repo => repo.DeleteAsync(existingTask), Times.Once);
            mockUnitOfWork.Verify(repo => repo.SaveChangesAsync(), Times.Once);
        }
        [Fact]
        public async Task DeleteTask_TaskDoesNotExist_ShouldReturnResultFailure()
        {
            //Arrange
            var mockTaskRepository = new Mock<ITaskRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var userId = Guid.NewGuid();
            var falseTaskId = Guid.NewGuid();

            mockTaskRepository.Setup(repo => repo.GetByIdAsync(falseTaskId)).ReturnsAsync((TaskItem?)null);

            var handler = new DeleteTaskCommandHandler(mockTaskRepository.Object, mockUnitOfWork.Object);
            var cmd = new DeleteTaskCommand(falseTaskId, userId);
            //Act
            var result = await handler.Handle(cmd, CancellationToken.None);
            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task not found");
            mockTaskRepository.Verify(repo => repo.DeleteAsync(It.IsAny<TaskItem>()), Times.Never);
            mockUnitOfWork.Verify(repo => repo.SaveChangesAsync(), Times.Never);

        }
        [Fact]
        public async Task DeleteTask_TaskExists_UserIsNotOwner_ShouldReturnResultFailure()
        {
            //Arrange
            var mockTaskRepository = new Mock<ITaskRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var userId = Guid.NewGuid();
            var existingTask = TaskItem.Create("test", "test", TaskPriority.High, userId);

            mockTaskRepository.Setup(repo => repo.GetByIdAsync(existingTask.Id)).ReturnsAsync(existingTask);

            var handler = new DeleteTaskCommandHandler(mockTaskRepository.Object, mockUnitOfWork.Object);
            var cmd = new DeleteTaskCommand(existingTask.Id, Guid.NewGuid());
            //Act
            var result = await handler.Handle(cmd, CancellationToken.None);
            //Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You are not allowed to delete this task"); // can do it with contain also 
            mockTaskRepository.Verify(repo => repo.DeleteAsync(existingTask), Times.Never);
            mockUnitOfWork.Verify(repo => repo.SaveChangesAsync(), Times.Never);
        }

    }
}
