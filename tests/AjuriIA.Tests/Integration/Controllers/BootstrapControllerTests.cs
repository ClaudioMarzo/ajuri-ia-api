using System.Net;
using System.Text.Json;
using AjuriIA.API.Controllers;
using AjuriIA.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AjuriIA.Tests.Integration.Controllers;

public class BootstrapControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact(DisplayName = "Given API running, When GET /api/bootstrap, Then returns HTTP 200")]
    public async Task Given_ApiRunning_When_GetBootstrap_Should_ReturnHttp200()
    {
        var response = await _client.GetAsync("/api/bootstrap");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Given API running, When GET /api/bootstrap, Then returns profiles and models")]
    public async Task Given_ApiRunning_When_GetBootstrap_Should_ReturnProfilesAndModels()
    {
        var response = await _client.GetAsync("/api/bootstrap");
        var body = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<BootstrapResponse>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        envelope.Should().NotBeNull();
        envelope!.Success.Should().BeTrue();
        envelope.Data.Should().NotBeNull();
        envelope.Data!.Profiles.Should().HaveCount(2);
        envelope.Data.Models.Default.Should().NotBeNullOrWhiteSpace();
        envelope.Data.Models.Models.Should().NotBeEmpty();
    }
}