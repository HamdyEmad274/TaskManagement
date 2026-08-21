using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger, RequestDelegate next)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(ValidationException ex)
            {
                _logger.LogWarning("Validation error occured");
                await HandleValidationExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                var problemDetails = new ProblemDetails
                {
                    Title = "An unexpected error occurred.",
                    Status = 500,
                    Detail = "An unexpected error occurred. Please try again later."
                };
                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        }

        private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
        {
            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = 400;
            
            var errors = ex.Errors
                .GroupBy(e=>e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            var problemDetails = new HttpValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
