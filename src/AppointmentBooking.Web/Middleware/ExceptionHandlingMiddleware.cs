using System.Net;
using System.Text.Json;
using AppointmentBooking.Core.Exceptions;

namespace AppointmentBooking.Web.Middleware;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ConflictException conflictEx => (HttpStatusCode.Conflict, conflictEx.Message),
            NotFoundException notFoundEx => (HttpStatusCode.NotFound, notFoundEx.Message),
            ForbiddenException forbiddenEx => (HttpStatusCode.Forbidden, forbiddenEx.Message),
            ValidationException valEx => (HttpStatusCode.BadRequest, valEx.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected server error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception caught by middleware.");
        }
        else
        {
            _logger.LogWarning("Domain exception caught: {Status} - {Message}", statusCode, message);
        }

        // If API request, respond with JSON
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var payload = new
            {
                status = (int)statusCode,
                error = message,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        else
        {
            // For MVC web pages, redirect to error page or set status
            context.Response.StatusCode = (int)statusCode;
            context.Items["ErrorMessage"] = message;
            context.Items["StatusCode"] = (int)statusCode;
            
            // Re-throw or redirect
            context.Response.Redirect($"/Home/Error?statusCode={(int)statusCode}&message={Uri.EscapeDataString(message)}");
        }
    }
}
