using System.Net;
using System.Text.Json;
using FluentValidation;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.API.Middlewares;

/// <summary>
/// Global exception handling middleware.
/// Domain ve Application katmanlarında fırlatılan exception'ları
/// uygun HTTP status code'larına ve tutarlı bir JSON formatına çevirir.
///
/// Mapping:
///  - FluentValidation.ValidationException        -> 400 Bad Request
///  - NotFoundException                           -> 404 Not Found
///  - UnauthorizedTaskOperationException          -> 403 Forbidden  (Resource Based Authorization)
///  - ForbiddenAccessException                    -> 403 Forbidden
///  - InvalidTaskStatusTransitionException        -> 409 Conflict
///  - UnauthorizedAccessException (Login hatası)  -> 401 Unauthorized
///  - Diğer tüm exception'lar                     -> 500 Internal Server Error
/// </summary>
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, errors) = exception switch
        {
            ValidationException validationException => (
                HttpStatusCode.BadRequest,
                "Doğrulama hatası.",
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            ),

            NotFoundException notFoundException => (
                HttpStatusCode.NotFound,
                notFoundException.Message,
                null
            ),

            UnauthorizedTaskOperationException unauthorizedTaskException => (
                HttpStatusCode.Forbidden,
                unauthorizedTaskException.Message,
                null
            ),

            ForbiddenAccessException forbiddenException => (
                HttpStatusCode.Forbidden,
                forbiddenException.Message,
                null
            ),

            InvalidTaskStatusTransitionException invalidTransitionException => (
                HttpStatusCode.Conflict,
                invalidTransitionException.Message,
                null
            ),

            UnauthorizedAccessException unauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                unauthorizedAccessException.Message,
                null
            ),

            _ => (
                HttpStatusCode.InternalServerError,
                "Beklenmeyen bir hata oluştu.",
                null
            )
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Beklenmeyen hata: {Message}", exception.Message);

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            title,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();
}
