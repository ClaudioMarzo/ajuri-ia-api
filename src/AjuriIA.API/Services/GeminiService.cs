using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AjuriIA.API.Services;

public class GeminiService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GeminiService> logger) : ILLMService
{
    private const string Model = "gemini-2.0-flash";
    private const int MaxAttempts = 2;

    public string Name => "gemini-flash";

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKey = configuration["GEMINI_API_KEY"]
            ?? throw new InvalidOperationException("GEMINI_API_KEY not configured");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:streamGenerateContent?alt=sse";

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userMessage } } } }
        };

        var response = await SendWithRetryAsync(apiKey, url, body, ct);

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null || !line.StartsWith("data: ")) continue;

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
                throw new HttpRequestException($"Gemini API error: {errorMsg}");
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

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string apiKey, string url, object body, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", apiKey);
            request.Content = JsonContent.Create(body);

            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if ((int)response.StatusCode != 429)
            {
                response.EnsureSuccessStatusCode();
                return response;
            }

            if (attempt == MaxAttempts)
            {
                response.EnsureSuccessStatusCode(); // lança HttpRequestException com 429
                return response;                    // nunca alcançado
            }

            // Respeita o Retry-After do Gemini; fallback de 5s
            var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(5);
            var waitMs = (int)Math.Min(retryAfter.TotalMilliseconds, 10_000);

            logger.LogWarning(
                "[Gemini] 429 Too Many Requests — aguardando {WaitMs}ms antes da tentativa {Next}/{Max}",
                waitMs, attempt + 1, MaxAttempts);

            await Task.Delay(waitMs, ct);
        }

        // nunca alcançado, mas satisfaz o compilador
        throw new HttpRequestException("Gemini: máximo de tentativas atingido");
    }
}
