namespace AjuriIA.API.Models;

public class ChatRequest
{
    public string ProfileId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Modelo preferido pelo cliente. Opcional — se vazio, usa o default do servidor.</summary>
    public string? Model { get; set; }
}
