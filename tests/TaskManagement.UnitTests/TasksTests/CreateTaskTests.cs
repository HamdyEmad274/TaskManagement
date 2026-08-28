using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.Commands.CreateTask;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.UnitTests.TasksTests
{
    public class CreateTaskTests
    {
        [Fact]
        public async Task Handle_ValidRequest_CreateTaskSuccessfully()
        {
            var mockTaskRepository = new Mock<ITaskRepository>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockTaskRepository.Setup(repo => repo.AddAsync(It.IsAny<TaskItem>()));
            mockUnitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()));

            var cmd = new CreateTaskCommand("Task 1", "Description 1", TaskPriority.High, Guid.Parse("43bfa581-60ea-4d38-8bc9-166827aaa972"));
            var handler = new CreateTaskCommandHandler(mockTaskRepository.Object, mockUnitOfWork.Object);

            var result = await handler.Handle(cmd, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();

            mockTaskRepository.Verify(repo => repo.AddAsync(It.Is<TaskItem>(t =>
                t.Title == cmd.Title &&
                t.Description == cmd.Description &&
                t.Priority == cmd.Priority &&
                t.UserId == cmd.UserId
            )), Times.Once);
            mockUnitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        }
    }
}
