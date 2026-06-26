namespace AjuriIA.API.Models;

public class ChatResponse
{
    public string RequestedLlm { get; set; } = string.Empty;
    public string LlmUsed { get; set; } = string.Empty;
    public bool FallbackUsed { get; set; }
    public string? FallbackFromLlm { get; set; }
    public string? FallbackMessage { get; set; }
    public string ProfileId { get; set; } = string.Empty;
}
