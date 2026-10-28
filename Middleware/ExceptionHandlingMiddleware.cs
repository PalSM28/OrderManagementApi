using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace OrderManagement.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                var (status, title) = Map(ex);

                if (status >= 500) _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
                else _logger.LogWarning("{Title}: {Message}", title, ex.Message);

                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    // only expose messages we wrote ourselves, never raw database errors
                    Detail = status < 500 && !(ex is DbUpdateException) ? ex.Message : null,
                    Instance = context.Request.Path
                };

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        }

        // C# 8: switch expression with type patterns (the "_" discard is required in C# 8).
        // Order matters: DbUpdateConcurrencyException derives from DbUpdateException.
        private static (int Status, string Title) Map(Exception ex) => ex switch
        {
            NotFoundException _ => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException _ => (StatusCodes.Status409Conflict, "Conflict"),
            BusinessRuleException _ => (StatusCodes.Status422UnprocessableEntity, "Business rule violated"),
            DbUpdateConcurrencyException _ => (StatusCodes.Status409Conflict, "The record was changed by someone else. Reload and retry."),
            DbUpdateException _ => (StatusCodes.Status409Conflict, "The database rejected the update"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
    }
}
