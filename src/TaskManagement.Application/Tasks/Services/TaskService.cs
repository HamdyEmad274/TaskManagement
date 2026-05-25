using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Application.Tasks.Interfaces;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Tasks.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;
        public TaskService(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TaskResponse>> CreateAsync(CreateTaskRequest request, Guid userId , CancellationToken cancellationToken = default)
        {
            var task = TaskItem.Create(request.Title, request.Description, request.Priority, userId);
            await _taskRepository.AddAsync(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var response = new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                CreatedAt = task.CreatedAt,
                UserId = task.UserId
            };
            return Result.Success(response);
        }
        public async Task<Result<TaskResponse>> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, Guid userId , CancellationToken cancellationToken = default)
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
            {
                return Result.Failure<TaskResponse>("Task not found ");
            }
            if (task.UserId != userId)
            {
                return Result.Failure<TaskResponse>("You are not allowed to update this task ");
            }
            task.UpdateStatus(request.Status);
            await _taskRepository.UpdateAsync(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var response = new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status.ToString(),
                Priority = task.Priority.ToString(),
                CreatedAt = task.CreatedAt,
                UserId = task.UserId
            };
            return Result.Success(response);
        }

        public Task<Result<IEnumerable<TaskResponse>>> GetAllByUserAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<Result<TaskResponse>> GetByIdAsync(Guid id, Guid userId)
        {
            throw new NotImplementedException();
        }

    }
}
