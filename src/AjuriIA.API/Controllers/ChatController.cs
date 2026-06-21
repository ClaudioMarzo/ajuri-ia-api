using System.Diagnostics;
using System.Text.Json;
using AjuriIA.API.Models;
using AjuriIA.API.Services;
using AjuriIA.API.Validators;
using Microsoft.AspNetCore.Mvc;

namespace AjuriIA.API.Controllers;

[ApiController]
[Route("api")]
public class ChatController(
    ProfileService profileService,
    LLMOrchestratorService orchestrator,
    ChatRequestValidator validator) : ControllerBase
{
    [HttpPost("chat")]
    public async Task StreamChat([FromBody] ChatRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Success = false,
                TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                MessageError = validation.Errors.First().ErrorMessage
            }, ct);
            return;
        }

        var profile = profileService.GetById(request.ProfileId);
        if (profile is null)
        {
            Response.StatusCode = 404;
            await Response.WriteAsJsonAsync(new ApiResponse<object>
            {
                Success = false,
                TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                MessageError = $"Perfil '{request.ProfileId}' não encontrado."
            }, ct);
            return;
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        await foreach (var chunk in orchestrator.StreamAsync(profile, request.Message, ct))
        {
            await Response.WriteAsync($"data: {chunk.Replace("\n", "\\n")}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        var donePayload = JsonSerializer.Serialize(new ApiResponse<ChatResponse>
        {
            Success = true,
            Data = new ChatResponse
            {
                LlmUsed = orchestrator.LastUsedLlm ?? "unknown",
                ProfileId = request.ProfileId
            },
            TraceId = traceId
        });

        await Response.WriteAsync($"data: [DONE] {donePayload}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
