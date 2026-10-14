using System.Net;
using System.Text.Json;

namespace Maydan.API.Middlewares;

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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt.");
            await HandleExceptionAsync(
                context,
                HttpStatusCode.Forbidden,
                "ليس لديك الصلاحية الكافية لتنفيذ هذا الإجراء.",
                ex.Message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(
                context,
                HttpStatusCode.InternalServerError,
                "حدث خطأ غير متوقع في الخادم.",
                "An unexpected error occurred on the server."
            );
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string messageAr, string messageEn)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success = false,
            messageAr = messageAr,
            messageEn = messageEn,
            data = (object?)null,
            statusCode = (int)statusCode
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, jsonOptions);

        await context.Response.WriteAsync(json);
    }
}