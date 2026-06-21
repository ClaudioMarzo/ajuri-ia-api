using System.Diagnostics;
using AjuriIA.API.Models;
using AjuriIA.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace AjuriIA.API.Controllers;

[ApiController]
[Route("api")]
public class ProfilesController(ProfileService profileService) : ControllerBase
{
    [HttpGet("profiles")]
    public IActionResult GetProfiles()
    {
        var profiles = profileService.GetAll();
        return Ok(new ApiResponse<IReadOnlyList<Profile>>
        {
            Success = true,
            Data = profiles,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy" });
}
