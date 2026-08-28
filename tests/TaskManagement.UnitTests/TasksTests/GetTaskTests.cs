using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.Queries.GetTaskById;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.UnitTests.TasksTests
{
    public class GetTaskTests
    {
        [Fact]
        public async Task Handle_GetTaskByUserId_Owner_ShouldSucceedAsync()
        {
            var mockTaskRepository = new Mock<ITaskRepository>();
            var userId = Guid.NewGuid();
            var existingTask = TaskItem.Create("test","test", TaskPriority.High , userId);

            mockTaskRepository.Setup(x => x.GetByIdAsync(existingTask.Id)).ReturnsAsync(existingTask);

            var query = new GetTaskByIdQuery(existingTask.Id,userId);

            var handler = new GetTaskByIdQueryHandler(mockTaskRepository.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Error.Should().BeEmpty();
            Assert.Equal(result.Value.Id, existingTask.Id);
        }

        [Fact]
        public async Task Handle_TaskNotFound_ShouldFailAsync()
        {
            var mockTaskRepository = new Mock<ITaskRepository>();

            var falseTaskId = Guid.NewGuid();

            mockTaskRepository.Setup(x => x.GetByIdAsync(falseTaskId)).ReturnsAsync((TaskItem?)null);

            var query = new GetTaskByIdQuery(falseTaskId, Guid.NewGuid());

            var handler = new GetTaskByIdQueryHandler(mockTaskRepository.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task not found");
        }

        [Fact]
        public async Task Handle_GetTaskByUserId_NotOwner_ShouldFailAsync()
        {
            var mockTaskRepository = new Mock<ITaskRepository>();

            var existingTask = TaskItem.Create("test", "test", TaskPriority.High, Guid.NewGuid());

            mockTaskRepository.Setup(x => x.GetByIdAsync(existingTask.Id)).ReturnsAsync(existingTask);

            var query = new GetTaskByIdQuery(existingTask.Id, Guid.NewGuid());

            var handler = new GetTaskByIdQueryHandler(mockTaskRepository.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("You are not allowed to view this task");
            

        }
    }
}
