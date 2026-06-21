namespace AjuriIA.API.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string? MessageError { get; set; }
}
