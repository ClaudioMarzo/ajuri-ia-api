using System.Net;
using System.Text;
using AjuriIA.API.Services;
using AjuriIA.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AjuriIA.Tests.Unit.Services;

public class GroqServiceTests
{
    private const string TestModel = "llama-3.3-70b-versatile";

    private static GroqService CreateSut(HttpMessageHandler handler)
    {
        var config = Substitute.For<IConfiguration>();
        config["GROQ_API_KEY"].Returns("gsk_test_key");

        var httpClient = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        return new GroqService(factory, config, NullLogger<GroqService>.Instance, TestModel);
    }

    private static HttpMessageHandler CreateSseHandler()
    {
        var content = string.Join("\n", new[]
        {
            "data: {\"choices\":[{\"delta\":{\"content\":\"Oi\"}}]}",
            "",
            "data: {\"choices\":[{\"delta\":{\"content\":\" da Groq\"}}]}",
            "",
            "data: [DONE]",
            ""
        });

        return new MockHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/event-stream")
            }));
    }

    [Fact(DisplayName = "Given Groq service, When created, Then Name is configured model")]
    public void Given_GroqService_When_Created_Should_HaveConfiguredName()
    {
        // Given / When
        var sut = CreateSut(CreateSseHandler());

        // Then
        sut.Name.Should().Be(TestModel);
    }

    [Fact(DisplayName = "Given valid Groq SSE response, When StreamAsync, Then yields streamed chunks")]
    public async Task Given_ValidSseResponse_When_StreamAsync_Should_YieldChunks()
    {
        // Given
        var sut = CreateSut(CreateSseHandler());
        var chunks = new List<string>();

        // When
        await foreach (var chunk in sut.StreamAsync("system", "user"))
            chunks.Add(chunk);

        // Then
        chunks.Should().Equal("Oi", " da Groq");
    }

    [Fact(DisplayName = "Given HTTP 429 response, When StreamAsync, Then throws HttpRequestException")]
    public async Task Given_Http429Response_When_StreamAsync_Should_ThrowHttpRequestException()
    {
        // Given
        var handler = new MockHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("{\"error\":\"rate_limit\"}", Encoding.UTF8, "application/json")
            }));
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
