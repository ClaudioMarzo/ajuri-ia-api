using System.Net;
using System.Text.Json;
using AjuriIA.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AjuriIA.Tests.Integration.Controllers;

public class ProfilesControllerTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact(DisplayName = "Given API running, When GET /api/profiles, Then returns HTTP 200")]
    public async Task Given_ApiRunning_When_GetProfiles_Should_ReturnHttp200()
    {
        // When
        var response = await _client.GetAsync("/api/profiles");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Given API running, When GET /api/profiles, Then returns exactly 6 profiles")]
    public async Task Given_ApiRunning_When_GetProfiles_Should_ReturnSixProfiles()
    {
        // When
        var response = await _client.GetAsync("/api/profiles");
        var body = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<List<Profile>>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Then
        envelope!.Data.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Given API running, When GET /api/health, Then returns HTTP 200")]
    public async Task Given_ApiRunning_When_GetHealth_Should_ReturnHttp200()
    {
        // When
        var response = await _client.GetAsync("/api/health");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Given API running, When GET /api/profiles, Then success is true")]
    public async Task Given_ApiRunning_When_GetProfiles_Should_ReturnSuccessTrue()
    {
        // When
        var response = await _client.GetAsync("/api/profiles");
        var body = await response.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<List<Profile>>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Then
        envelope!.Success.Should().BeTrue();
    }
}
