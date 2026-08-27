using FluentAssertions;
using Moq;
using TaskManagement.Application.Tasks.Commands.UpdateTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.UnitTests.TasksTests
{
    public class UpdateTaskTests
    {
        [Fact]
        public async Task Handle_ValidRequest_UpdateTaskSuccessfully()
        {
            var mockTaskRepository = new Mock<ITaskRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var existingTask = TaskItem.Create("Task 1", "Description 1", TaskPriority.High, Guid.Parse("43bfa581-60ea-4d38-8bc9-166827aaa972"));
            
            mockTaskRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);
            mockTaskRepository.Setup(repo => repo.UpdateAsync(It.IsAny<TaskItem>()));
            mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var cmd = new UpdateTaskStatusCommand(Guid.NewGuid(),AppTaskStatus.InProgress, Guid.Parse("43bfa581-60ea-4d38-8bc9-166827aaa972"));
            
            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepository.Object, mockUnitOfWork.Object);

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            existingTask.Status.Should().Be(cmd.Status);


            mockTaskRepository.Verify(repo => repo.UpdateAsync(It.Is<TaskItem>(t=>t.Status == cmd.Status)),
                Times.Once);
            mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        }
        [Fact]
        public async Task Handle_TaskNotFound_UpdateTaskFailed()
        {
            // Arrange
            var mockTaskRepository = new Mock<ITaskRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockTaskRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TaskItem?)null);

            var cmd = new UpdateTaskStatusCommand(Guid.NewGuid(), AppTaskStatus.InProgress, Guid.Parse("43bfa581-60ea-4d38-8bc9-166827aaa972"));

            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepository.Object, mockUnitOfWork.Object);

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task not found");
            mockTaskRepository.Verify(repo => repo.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
            mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        }
        [Fact]
        public async Task Handle_UserDoesNotOwnTheTask_UpdateTaskFailed()
        {
            // Arrange
            var mockTaskRepository = new Mock<ITaskRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            var existingTask = TaskItem.Create("Task 1", "Description 1", TaskPriority.High, Guid.Parse("43bfa581-60ea-4d38-8bc9-166827aaa972"));

            mockTaskRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(existingTask);



            var cmd = new UpdateTaskStatusCommand(existingTask.Id, AppTaskStatus.InProgress, Guid.NewGuid());

            var handler = new UpdateTaskStatusCommandHandler(mockTaskRepository.Object, mockUnitOfWork.Object);

            // Act
            var result = await handler.Handle(cmd, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You are not allowed to update this task");
            existingTask.Status.Should().Be(AppTaskStatus.Pending);
            mockTaskRepository.Verify(repo => repo.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
            mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        }
    }
}
