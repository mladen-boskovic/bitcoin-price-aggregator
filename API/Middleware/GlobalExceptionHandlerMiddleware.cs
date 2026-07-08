using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace API.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            catch (ValidationException ex)
            {
                var messages = ex.Errors.Select(e => e.ErrorMessage).ToList();
                await WriteErrorResponse(context, HttpStatusCode.BadRequest, "Validation failed", string.Join("; ", messages));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "External API request failed");
                var statusCode = ex.StatusCode ?? HttpStatusCode.BadGateway;
                await WriteErrorResponse(context, statusCode, "External API error", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Price aggregation failed");
                await WriteErrorResponse(context, HttpStatusCode.ServiceUnavailable, "Service unavailable", ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred");
                await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "An unexpected error occurred", "Contact Support");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "An unexpected error occurred", "Contact Support");
            }
        }

        private static Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, string error, string detail)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var body = JsonSerializer.Serialize(new
            {
                statusCode = (int)statusCode,
                title = error,
                detail
            });

            return context.Response.WriteAsync(body);
        }
    }
}
