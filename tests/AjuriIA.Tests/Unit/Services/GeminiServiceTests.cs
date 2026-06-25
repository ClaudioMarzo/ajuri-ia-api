using System.Net;
using System.Text;
using AjuriIA.API.Services;
using AjuriIA.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AjuriIA.Tests.Unit.Services;

public class GeminiServiceTests
{
    private const string TestModel = "gemini-2.5-flash";

    private static GeminiService CreateSut(HttpMessageHandler handler)
    {
        var config = Substitute.For<IConfiguration>();
        config["GEMINI_API_KEY"].Returns("AIza-test-fake-key");
        var httpClient = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        return new GeminiService(factory, config, NullLogger<GeminiService>.Instance, TestModel);
    }

    private static HttpMessageHandler CreateSseHandler(IEnumerable<string> textChunks)
    {
        var sb = new StringBuilder();
        foreach (var text in textChunks)
        {
            var escapedText = text.Replace("\"", "\\\"");
            sb.AppendLine($"data: {{\"candidates\":[{{\"content\":{{\"parts\":[{{\"text\":\"{escapedText}\"}}],\"role\":\"model\"}}}}]}}");
            sb.AppendLine();
        }

        var content = sb.ToString();
        return new MockHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/event-stream")
            }));
    }

    [Fact(DisplayName = "Given Gemini service, When created, Then Name is the model id")]
    public void Given_GeminiService_When_Created_Should_HaveCorrectName()
    {
        // Given / When
        var sut = CreateSut(CreateSseHandler([]));

        // Then
        sut.Name.Should().Be(TestModel);
    }

    [Fact(DisplayName = "Given valid SSE response, When StreamAsync, Then yields correct chunk count")]
    public async Task Given_ValidSseResponse_When_StreamAsync_Should_YieldCorrectChunkCount()
    {
        // Given
        var sut = CreateSut(CreateSseHandler(["Olá", " mundo"]));
        var chunks = new List<string>();

        // When
        await foreach (var chunk in sut.StreamAsync("system", "user"))
            chunks.Add(chunk);

        // Then
        chunks.Should().HaveCount(2);
    }

    [Fact(DisplayName = "Given HTTP 403 response, When StreamAsync, Then throws HttpRequestException")]
    public async Task Given_Http403Response_When_StreamAsync_Should_ThrowHttpRequestException()
    {
        // Given
        var handler = new MockHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)));
        var sut = CreateSut(handler);

        // When
        var act = async () =>
        {
            await foreach (var _ in sut.StreamAsync("system", "user")) { }
        };

        // Then
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
