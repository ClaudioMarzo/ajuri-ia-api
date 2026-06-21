using System.Net;
using System.Text;
using AjuriIA.API.Services;
using AjuriIA.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace AjuriIA.Tests.Unit.Services;

public class ClaudeServiceTests
{
    private static ClaudeService CreateSut(HttpMessageHandler handler)
    {
        var config = Substitute.For<IConfiguration>();
        config["CLAUDE_API_KEY"].Returns("sk-test-fake-key");
        var httpClient = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        return new ClaudeService(factory, config);
    }

    private static HttpMessageHandler CreateSseHandler(IEnumerable<string> textChunks)
    {
        var sb = new StringBuilder();
        foreach (var text in textChunks)
        {
            var escapedText = text.Replace("\"", "\\\"");
            sb.AppendLine("event: content_block_delta");
            sb.AppendLine($"data: {{\"type\":\"content_block_delta\",\"index\":0,\"delta\":{{\"type\":\"text_delta\",\"text\":\"{escapedText}\"}}}}");
            sb.AppendLine();
        }
        sb.AppendLine("event: message_stop");
        sb.AppendLine("data: {\"type\":\"message_stop\"}");
        sb.AppendLine();

        var content = sb.ToString();
        return new MockHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/event-stream")
            }));
    }

    [Fact(DisplayName = "Given valid API key, When StreamAsync, Then Name is claude-haiku")]
    public void Given_ValidApiKey_When_CreateService_Should_HaveCorrectName()
    {
        // Given / When
        var sut = CreateSut(CreateSseHandler([]));

        // Then
        sut.Name.Should().Be("claude-haiku");
    }

    [Fact(DisplayName = "Given valid SSE response, When StreamAsync, Then yields text chunks")]
    public async Task Given_ValidSseResponse_When_StreamAsync_Should_YieldTextChunks()
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

    [Fact(DisplayName = "Given valid SSE response, When StreamAsync, Then first chunk is correct text")]
    public async Task Given_ValidSseResponse_When_StreamAsync_Should_ReturnCorrectFirstChunk()
    {
        // Given
        var sut = CreateSut(CreateSseHandler(["Olá", " mundo"]));
        var chunks = new List<string>();

        // When
        await foreach (var chunk in sut.StreamAsync("system", "user"))
            chunks.Add(chunk);

        // Then
        chunks[0].Should().Be("Olá");
    }

    [Fact(DisplayName = "Given HTTP 500 response, When StreamAsync, Then throws HttpRequestException")]
    public async Task Given_Http500Response_When_StreamAsync_Should_ThrowHttpRequestException()
    {
        // Given
        var handler = new MockHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
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
