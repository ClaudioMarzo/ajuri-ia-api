namespace AjuriIA.API.Models;

/// <summary>
/// Configuração do provedor OpenRouter usada como fallback quando os modelos Gemini falham.
/// </summary>
public class OpenRouterOptions
{
    /// <summary>
    /// Lista ordenada de modelos OpenRouter para fallback.
    /// </summary>
    public List<OpenRouterModel> Models { get; set; } =
    [
        new() { Id = "openai/gpt-4o-mini", Label = "GPT-4o Mini" },
        new() { Id = "anthropic/claude-3.5-sonnet", Label = "Claude 3.5 Sonnet" },
        new() { Id = "meta-llama/llama-3.1-70b-instruct", Label = "Llama 3.1 70B Instruct" }
    ];

    /// <summary>Modelo preferencial quando o cliente pedir explicitamente um modelo OpenRouter.</summary>
    public string? Default { get; set; }

    public string DefaultModelId =>
        !string.IsNullOrWhiteSpace(Default) ? Default! : Models.FirstOrDefault()?.Id ?? string.Empty;

    public bool IsAllowed(string? modelId) =>
        !string.IsNullOrWhiteSpace(modelId) &&
        Models.Any(m => m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

    /// <summary>Header opcional HTTP-Referer recomendado pelo OpenRouter.</summary>
    public string? SiteUrl { get; set; }

    /// <summary>Header opcional X-Title recomendado pelo OpenRouter.</summary>
    public string? AppName { get; set; }
}

public class OpenRouterModel
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
