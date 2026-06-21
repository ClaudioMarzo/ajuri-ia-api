namespace AjuriIA.API.Services;

public interface ILLMService
{
    string Name { get; }
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}
