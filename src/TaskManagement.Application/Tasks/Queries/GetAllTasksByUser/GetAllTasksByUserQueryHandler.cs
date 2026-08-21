using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Tasks.Queries.GetAllTasksByUser
{
    public class GetAllTasksByUserQueryHandler : IRequestHandler<GetAllTasksByUserQuery, Result<IEnumerable<TaskResponse>>>
    {
        private readonly ITaskRepository _taskRepository;

        public GetAllTasksByUserQueryHandler(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
        }

        public async Task<Result<IEnumerable<TaskResponse>>> Handle(GetAllTasksByUserQuery request, CancellationToken cancellationToken)
        {
            var tasks = await _taskRepository.GetAllByUserIdAsync(request.UserId);
            var taskItems = new List<TaskResponse>();
            foreach (var task in tasks)
            {
                taskItems.Add(new TaskResponse
                {
                    Id = task.Id,
                    UserId = task.UserId,
                    Title = task.Title,
                    Description = task.Description,
                    Status = task.Status.ToString(),
                    Priority = task.Priority.ToString(),
                    CreatedAt = task.CreatedAt
                });
            }
            return Result.Success<IEnumerable<TaskResponse>>(taskItems);
        }
    }
}
