using System.Diagnostics;
using AjuriIA.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace AjuriIA.API.Controllers;

[ApiController]
[Route("api")]
public class ModelsController(GeminiOptions geminiOptions) : ControllerBase
{
    [HttpGet("models")]
    public IActionResult GetModels() =>
        Ok(new ApiResponse<ModelsResponse>
        {
            Success = true,
            Data = new ModelsResponse
            {
                Default = geminiOptions.DefaultModelId,
                Models = geminiOptions.Models
            },
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
}

public class ModelsResponse
{
    public string Default { get; set; } = string.Empty;
    public IReadOnlyList<GeminiModel> Models { get; set; } = [];
}
