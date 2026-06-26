using System.Diagnostics;
using AjuriIA.API.Models;
using AjuriIA.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AjuriIA.API.Controllers;

[ApiController]
[Route("api")]
public class BootstrapController(
    ProfileService profileService,
    GeminiOptions geminiOptions) : ControllerBase
{
    [HttpGet("bootstrap")]
    public IActionResult GetBootstrap() =>
        Ok(new ApiResponse<BootstrapResponse>
        {
            Success = true,
            Data = new BootstrapResponse
            {
                Profiles = profileService.GetAll(),
                Models = new ModelsResponse
                {
                    Default = geminiOptions.DefaultModelId,
                    Models = geminiOptions.Models
                }
            },
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
}

public class BootstrapResponse
{
    public IReadOnlyList<Profile> Profiles { get; set; } = [];
    public ModelsResponse Models { get; set; } = new();
}