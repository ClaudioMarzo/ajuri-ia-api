namespace AjuriIA.API.Models;

/// <summary>
/// Configuração do provedor Groq usado como fallback após Gemini.
/// </summary>
public class GroqOptions
{
    /// <summary>
    /// Lista ordenada de modelos Groq para fallback.
    /// </summary>
    public List<GroqModel> Models { get; set; } =
    [
        new() { Id = "llama-3.3-70b-versatile", Label = "Llama 3.3 70B Versatile" },
        new() { Id = "llama-3.1-8b-instant", Label = "Llama 3.1 8B Instant" }
    ];

    /// <summary>Modelo preferencial quando o cliente pedir explicitamente um modelo Groq.</summary>
    public string? Default { get; set; }

    public string DefaultModelId =>
        !string.IsNullOrWhiteSpace(Default) ? Default! : Models.FirstOrDefault()?.Id ?? string.Empty;

    public bool IsAllowed(string? modelId) =>
        !string.IsNullOrWhiteSpace(modelId) &&
        Models.Any(m => m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
}

public class GroqModel
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
