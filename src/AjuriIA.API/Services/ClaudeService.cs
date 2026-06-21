using System.Runtime.CompilerServices;
using System.Text.Json;

namespace AjuriIA.API.Services;

public class ClaudeService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ILLMService
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-haiku-4-5-20251001";

    public string Name => "claude-haiku";

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKey = configuration["CLAUDE_API_KEY"]
            ?? throw new InvalidOperationException("CLAUDE_API_KEY not configured");

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = Model,
            max_tokens = 1024,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } },
            stream = true
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
            if (json == "[DONE]") break;

            JsonElement root;
            try { root = JsonDocument.Parse(json).RootElement; }
            catch (JsonException) { continue; }

            if (root.TryGetProperty("type", out var typeProp) &&
                typeProp.GetString() == "content_block_delta" &&
                root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var text))
            {
                var chunk = text.GetString();
                if (chunk is not null) yield return chunk;
            }
        }
    }
}
