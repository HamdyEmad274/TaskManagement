using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TaskManagement.Application.Common.Behaviours
{
    public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
        public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("Starting Request: {RequestName}", requestName);
            var time = Stopwatch.StartNew();
            try
            {
                var response = await next();
                time.Stop();
                _logger.LogInformation(
                    "Finished Request: {RequestName} in {ElapsedMilliseconds}ms",
                    requestName, time.ElapsedMilliseconds);
                return response;
            }
            catch (ValidationException ex)
            {
                time.Stop();
                _logger.LogWarning(
                    "Validation failed for {RequestName} after {ElapsedMilliseconds}ms with {ErrorCount} errors",
                    requestName,
                    time.ElapsedMilliseconds,
                    ex.Errors.Count());
                throw;
            }
            catch (Exception ex)
            {
                time.Stop();
                _logger.LogError(
                    ex,
                    "Request: {RequestName} failed in {ElapsedMilliseconds}ms",
                    requestName,
                    time.ElapsedMilliseconds);
                throw;
            }

        }
    }
}
