using System.Diagnostics;
using AjuriIA.API.Models;

namespace AjuriIA.API.Middleware;

public class ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Exception after response started — cannot send error envelope. TraceId: {TraceId}", traceId);
                return;
            }

            logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", traceId);

            context.Response.StatusCode = ex is AllLLMsUnavailableException ? 503 : 500;
            context.Response.ContentType = "application/json";

            var response = new ApiResponse<object>
            {
                Success = false,
                TraceId = traceId,
                MessageError = ex is AllLLMsUnavailableException
                    ? ex.Message
                    : "Ocorreu um erro inesperado. Tente novamente."
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
