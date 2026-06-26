using System.Net;
using System.Text;
using System.Text.Json;
using AjuriIA.API.Controllers;
using AjuriIA.API.Models;
using AjuriIA.API.Services;
using AjuriIA.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AjuriIA.Tests.Integration.Controllers;

public class ChatControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static async IAsyncEnumerable<string> MockStream(params string[] chunks)
    {
        foreach (var chunk in chunks) yield return chunk;
        await Task.CompletedTask;
    }

    private HttpClient CreateClientWithMockedLLM(string llmName = "mock-llm", string[] chunks = null!)
    {
        chunks ??= ["Resposta de teste"];
        return _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(ILLMService)).ToList();
                foreach (var d in descriptors) services.Remove(d);

                var mock = Substitute.For<ILLMService>();
                mock.Name.Returns(llmName);
                mock.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(MockStream(chunks));
                services.AddSingleton(mock);
            });
        }).CreateClient();
    }

    private HttpClient CreateClientWithFallbackScenario(string primaryLlm, string fallbackLlm, string[] chunks)
    {
        return _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(ILLMService)).ToList();
                foreach (var d in descriptors) services.Remove(d);

                var primary = Substitute.For<ILLMService>();
                primary.Name.Returns(primaryLlm);
                primary.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(_ => throw new HttpRequestException());

                var fallback = Substitute.For<ILLMService>();
                fallback.Name.Returns(fallbackLlm);
                fallback.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(MockStream(chunks));

                services.AddSingleton(primary);
                services.AddSingleton(fallback);
            });
        }).CreateClient();
    }

    [Fact(DisplayName = "Given invalid profileId, When POST /api/chat, Then returns HTTP 400")]
    public async Task Given_InvalidProfileId_When_PostChat_Should_ReturnHttp400()
    {
        // Given
        var client = CreateClientWithMockedLLM();
        var payload = new { profileId = "perfil-invalido", message = "mensagem válida" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        var response = await client.PostAsync("/api/chat", content);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Given message too short, When POST /api/chat, Then returns HTTP 400")]
    public async Task Given_MessageTooShort_When_PostChat_Should_ReturnHttp400()
    {
        // Given
        var client = CreateClientWithMockedLLM();
        var payload = new { profileId = "professor", message = "ab" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        var response = await client.PostAsync("/api/chat", content);

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Given valid request with mocked LLM, When POST /api/chat, Then returns text/event-stream")]
    public async Task Given_ValidRequest_When_PostChat_Should_ReturnEventStreamContentType()
    {
        // Given
        var client = CreateClientWithMockedLLM();
        var payload = new { profileId = "professor", message = "Crie uma aula sobre guaraná" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        var response = await client.PostAsync("/api/chat", content);

        // Then
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
    }

    [Fact(DisplayName = "Given valid request with mocked LLM, When POST /api/chat, Then response contains DONE event")]
    public async Task Given_ValidRequest_When_PostChat_Should_ContainDoneEvent()
    {
        // Given
        var client = CreateClientWithMockedLLM(chunks: ["Olá", " mundo"]);
        var payload = new { profileId = "professor", message = "Crie uma aula sobre guaraná" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        var response = await client.PostAsync("/api/chat", content);
        var body = await response.Content.ReadAsStringAsync();

        // Then
        body.Should().Contain("[DONE]");
    }

    [Fact(DisplayName = "Given primary LLM fails, When POST /api/chat, Then DONE event contains fallback metadata")]
    public async Task Given_PrimaryLLMFails_When_PostChat_Should_ContainFallbackMetadata()
    {
        var client = CreateClientWithFallbackScenario("gemini-2.5-flash", "llama-3.3-70b-versatile", ["Resposta por fallback"]);
        var payload = new
        {
            profileId = "professor",
            message = "Como posso comecar?",
            model = "gemini-2.5-flash"
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/chat", content);
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("\"fallbackUsed\":true");
        body.Should().Contain("\"fallbackFromLlm\":\"gemini-2.5-flash\"");
        body.Should().Contain("llama-3.3-70b-versatile");
        body.Should().Contain("falhou; resposta gerada com fallback");
    }

    [Fact(DisplayName = "Given valid request with mocked LLM, When POST /api/chat, Then [CHAT] log is emitted with profile info")]
    public async Task Given_ValidRequest_When_PostChat_Should_EmitChatLogWithProfileInfo()
    {
        // Given
        var captureLogger = new CaptureLogger<ChatController>();

        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var llmDescriptors = services.Where(d => d.ServiceType == typeof(ILLMService)).ToList();
                foreach (var d in llmDescriptors) services.Remove(d);

                var loggerDescriptors = services.Where(d => d.ServiceType == typeof(ILogger<ChatController>)).ToList();
                foreach (var d in loggerDescriptors) services.Remove(d);

                var mock = Substitute.For<ILLMService>();
                mock.Name.Returns("mock-llm");
                mock.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(MockStream("Resposta de teste"));
                services.AddSingleton(mock);

                services.AddSingleton<ILogger<ChatController>>(captureLogger);
            });
        }).CreateClient();

        var payload = new { profileId = "professor", message = "Crie uma aula sobre guaraná" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        await client.PostAsync("/api/chat", content);

        // Then
        captureLogger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("[CHAT]") &&
            e.Message.Contains("profile="));
    }
}
