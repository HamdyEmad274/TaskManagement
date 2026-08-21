using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Common;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Application.Tasks.Commands.DeleteTask
{
    public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteTaskCommandHandler(ITaskRepository taskRepository, IUnitOfWork unitOfWork)
        {
            _taskRepository = taskRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
        {
            var task = await _taskRepository.GetByIdAsync(request.Id);
            if (task == null)
                return Result.Failure("Task not found");

            // Ownership check (IDOR guard) — mirrors GetTaskById/UpdateTaskStatus handlers.
            if (task.UserId != request.UserId)
                return Result.Failure("You are not allowed to delete this task");

            await _taskRepository.DeleteAsync(task);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success("Task deleted successfully");
        }
    }
}
