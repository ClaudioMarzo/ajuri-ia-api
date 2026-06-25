namespace AjuriIA.API.Models;

/// <summary>
/// Catálogo de modelos Gemini permitidos, configurado em "Gemini" no appsettings.
/// A ordem de <see cref="Models"/> define a ordem de fallback após o modelo escolhido.
/// </summary>
public class GeminiOptions
{
    public List<GeminiModel> Models { get; set; } = [];

    /// <summary>Modelo usado quando o cliente não informa um. Cai no primeiro da lista se vazio.</summary>
    public string? Default { get; set; }

    public string DefaultModelId =>
        !string.IsNullOrWhiteSpace(Default) ? Default! : Models.FirstOrDefault()?.Id ?? string.Empty;

    public bool IsAllowed(string? modelId) =>
        !string.IsNullOrWhiteSpace(modelId) &&
        Models.Any(m => m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));
}

public class GeminiModel
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
