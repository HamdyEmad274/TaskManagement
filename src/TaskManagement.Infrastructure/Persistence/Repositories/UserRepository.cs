using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces.Repositories;

namespace TaskManagement.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _appDbContext;

        public UserRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public async Task AddAsync(User user)
        {
            await _appDbContext.Users.AddAsync(user);
        }

        public Task DeleteAsync(User user)
        {
            _appDbContext.Users.Remove(user);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            var users = await _appDbContext.Users.ToListAsync();
            return users;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var user = await _appDbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user;
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            var user = await _appDbContext.Users.FindAsync(id);
            return user;
        }
    }
}
