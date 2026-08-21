using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Infrastructure.Persistence.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _appDbContext;

        public TaskRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task AddAsync(TaskItem task)
        {
            await _appDbContext.Tasks.AddAsync(task);
        }

        public async Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(Guid userId)
        {
            var tasks = await _appDbContext.Tasks
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
            return tasks;
        }

        public async Task<TaskItem?> GetByIdAsync(Guid id)
        {
            var task = await _appDbContext.Tasks.FindAsync(id);
            return task;
        }

        public Task UpdateAsync(TaskItem task)
        {
            _appDbContext.Tasks.Update(task);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TaskItem task)
        {
            _appDbContext.Tasks.Remove(task);
            return Task.CompletedTask;
        }
    }
}
