using System.Text;
using System.Text.Json;
using AjuriIA.API.Models;
using AjuriIA.API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AjuriIA.Tests.Functional;

public class ChatFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatFlowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static async IAsyncEnumerable<string> MockStream(params string[] chunks)
    {
        foreach (var c in chunks) yield return c;
        await Task.CompletedTask;
    }

    private HttpClient CreateClientWithMockedLLM(string llmName, string[] chunks)
    {
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

    [Fact(DisplayName = "Given professor profile with mocked LLM, When full chat flow, Then SSE body contains llmUsed in DONE event")]
    public async Task Given_ProfessorProfile_When_FullChatFlow_Should_ContainLlmUsedInDoneEvent()
    {
        // Given
        var client = CreateClientWithMockedLLM("claude-haiku", ["Aula", " de", " guaraná"]);
        var payload = new { profileId = "professor", message = "Crie uma aula sobre guaraná para o 5º ano" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        var response = await client.PostAsync("/api/chat", content);
        var body = await response.Content.ReadAsStringAsync();

        // Then
        body.Should().Contain("claude-haiku");
    }

    [Fact(DisplayName = "Given professor profile with mocked LLM, When full chat flow, Then last SSE data line is DONE event")]
    public async Task Given_ProfessorProfile_When_FullChatFlow_Should_HaveDoneAsLastDataLine()
    {
        // Given
        var client = CreateClientWithMockedLLM("claude-haiku", ["resposta"]);
        var payload = new { profileId = "professor", message = "Crie uma aula sobre guaraná" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        var response = await client.PostAsync("/api/chat", content);
        var body = await response.Content.ReadAsStringAsync();
        var lastDataLine = body.Split('\n')
                               .Where(l => l.StartsWith("data:"))
                               .LastOrDefault(l => !string.IsNullOrWhiteSpace(l));

        // Then
        lastDataLine.Should().Contain("[DONE]");
    }

    [Fact(DisplayName = "Given all LLMs failing, When full chat flow, Then response does not expose stack trace")]
    public async Task Given_AllLLMsFailing_When_FullChatFlow_Should_NotExposeStackTrace()
    {
        // Given
        var client = _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(ILLMService)).ToList();
                foreach (var d in descriptors) services.Remove(d);

                var mock = Substitute.For<ILLMService>();
                mock.Name.Returns("failing-llm");
                mock.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Throws<HttpRequestException>();
                services.AddSingleton(mock);
            });
        }).CreateClient();

        var payload = new { profileId = "professor", message = "Crie uma aula sobre guaraná" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // When
        var response = await client.PostAsync("/api/chat", content);
        var body = await response.Content.ReadAsStringAsync();

        // Then
        body.Should().NotContain("StackTrace");
    }
}
