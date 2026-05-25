using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
        {
            if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
                return;

            var admin = User.Create(
                name: "Admin",
                email: "admin@taskmanagement.com",
                passwordHash: passwordHasher.Hash("Admin@123"),
                role: UserRole.Admin
            );

            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();

        }
    }
}
