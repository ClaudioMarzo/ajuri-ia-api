using Serilog.Context;
using System.Text.Json;

namespace AjuriIA.API.Middleware;

public class RequestEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.EnableBuffering();

        var profileId = "unknown";
        try
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body);
            if (doc.RootElement.TryGetProperty("profileId", out var prop))
                profileId = prop.GetString() ?? "unknown";
        }
        catch { /* body não é JSON válido — profileId fica como "unknown" */ }
        finally
        {
            context.Request.Body.Seek(0, SeekOrigin.Begin);
        }

        using (LogContext.PushProperty("ProfileId", profileId))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        using (LogContext.PushProperty("ClientIp", context.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
        {
            await next(context);
        }
    }
}
