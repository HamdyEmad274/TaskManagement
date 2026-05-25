using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Application.Tasks.Interfaces;
using TaskManagement.Application.Tasks.Services;
using TaskManagement.Application.Users.Interfaces;
using TaskManagement.Application.Users.Services;
using TaskManagement.Domain.Interfaces;
using TaskManagement.Domain.Interfaces.Repositories;
using TaskManagement.Infrastructure.Auth;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;

namespace TaskManagement.Infrastructure.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(options => jwtSection.Bind(options));
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

            return services;
        }
    }
}
