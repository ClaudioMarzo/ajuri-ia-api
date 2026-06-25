using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AjuriIA.API.Services;

/// <summary>
/// Uma instância por modelo Gemini. O fallback entre modelos é feito pelo
/// <see cref="LLMOrchestratorService"/>, que registra cada modelo como um ILLMService.
/// </summary>
public class GeminiService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GeminiService> logger,
    string model) : ILLMService
{
    public string Name => model;

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKey = configuration["GEMINI_API_KEY"]
            ?? throw new InvalidOperationException("GEMINI_API_KEY not configured");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?alt=sse";

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userMessage } } } }
        };

        var client = httpClientFactory.CreateClient("gemini");
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(body);

        // O pipeline de resiliência (Polly) trata retry de rede/timeout/429.
        // O fallback de modelo em 503 (sobrecarga) é responsabilidade do orquestrador.
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            // Surfaceia o corpo de erro do Gemini (motivo real: chave inválida,
            // modelo sobrecarregado, quota etc.) em vez de descartá-lo.
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if (errorBody.Length > 500) errorBody = errorBody[..500];
            logger.LogWarning(
                "[Gemini] modelo {Model} retornou {Status}: {Body}",
                model, (int)response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Gemini {(int)response.StatusCode} ({response.StatusCode}) no modelo {model}: {errorBody}",
                inner: null,
                statusCode: response.StatusCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal)) continue;

            var json = line[6..];

            JsonElement root;
            try { root = JsonDocument.Parse(json).RootElement; }
            catch (JsonException) { continue; }

            // Detecta erro retornado no corpo do SSE (ex: quota, permissão)
            if (root.TryGetProperty("error", out var error))
            {
                var errorMsg = error.TryGetProperty("message", out var msg)
                    ? msg.GetString() ?? "Gemini API error"
                    : "Gemini API error";
                throw new HttpRequestException($"Gemini API error no modelo {model}: {errorMsg}");
            }

            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var textProp))
            {
                var chunk = textProp.GetString();
                if (chunk is not null) yield return chunk;
            }
        }
    }
}
