using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AjuriIA.API.Services;

public class GeminiService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ILLMService
{
    private const string Model = "gemini-2.0-flash";

    public string Name => "gemini-flash";

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKey = configuration["GEMINI_API_KEY"]
            ?? throw new InvalidOperationException("GEMINI_API_KEY not configured");

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:streamGenerateContent?alt=sse&key={apiKey}";

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url);

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = userMessage } } } }
        };

        request.Content = JsonContent.Create(body);
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

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
