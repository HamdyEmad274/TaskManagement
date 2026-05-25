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
    }
}
