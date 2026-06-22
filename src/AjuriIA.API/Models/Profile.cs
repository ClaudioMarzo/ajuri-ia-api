namespace AjuriIA.API.Models;

public class Profile
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Icone { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Llm { get; set; } = string.Empty;
    public string IdealLlm { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
}
