using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Tasks.DTOs;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Tasks.Commands.UpdateTask
{
    public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand, Result<TaskResponse>>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTaskStatusCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TaskResponse>> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            if (task == null)
            {
                return Result.Failure<TaskResponse>("Task not found");
            }
            if (task.UserId != request.UserId)
            {
                return Result.Failure<TaskResponse>("You are not allowed to update this task");
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
    }
}
