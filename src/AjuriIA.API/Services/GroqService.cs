using System.Runtime.CompilerServices;
using System.Text.Json;
using AjuriIA.API.Models;
using Microsoft.Extensions.Logging;

namespace AjuriIA.API.Services;

/// <summary>
/// Provider Groq usado como fallback prioritário após Gemini.
/// </summary>
public class GroqService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<GroqService> logger,
    string model) : ILLMService
{
    public string Name => model;

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKey = configuration["GROQ_API_KEY"]
            ?? throw new InvalidOperationException("GROQ_API_KEY not configured");

        var body = new
        {
            model,
            stream = true,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            }
        };

        var client = httpClientFactory.CreateClient("groq");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(body);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if (errorBody.Length > 500) errorBody = errorBody[..500];
            logger.LogWarning(
                "[Groq] modelo {Model} retornou {Status}: {Body}",
                model,
                (int)response.StatusCode,
                errorBody);
            throw new HttpRequestException(
                $"Groq {(int)response.StatusCode} ({response.StatusCode}) no modelo {model}: {errorBody}",
                inner: null,
                statusCode: response.StatusCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;

            var payload = line[5..].Trim();
            if (string.Equals(payload, "[DONE]", StringComparison.Ordinal))
                yield break;
            if (string.IsNullOrWhiteSpace(payload))
                continue;

            JsonElement root;
            try { root = JsonDocument.Parse(payload).RootElement; }
            catch (JsonException) { continue; }

            if (root.TryGetProperty("error", out var error))
            {
                var errorMsg = error.TryGetProperty("message", out var msg)
                    ? msg.GetString() ?? "Groq API error"
                    : "Groq API error";
                throw new HttpRequestException($"Groq API error no modelo {model}: {errorMsg}");
            }

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var content))
            {
                var chunk = content.GetString();
                if (!string.IsNullOrEmpty(chunk))
                    yield return chunk;
            }
        }
    }
}
