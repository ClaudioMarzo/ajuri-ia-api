using System.Net;
using System.Text;
using AjuriIA.API.Models;
using AjuriIA.API.Services;
using AjuriIA.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace AjuriIA.Tests.Unit.Services;

public class OpenRouterServiceTests
{
    private const string TestModel = "openai/gpt-4o-mini";

    private static OpenRouterService CreateSut(HttpMessageHandler handler)
    {
        var config = Substitute.For<IConfiguration>();
        config["OPENROUTER_API_KEY"].Returns("or-test-key");

        var options = new OpenRouterOptions
        {
            Default = TestModel,
            Models = [new OpenRouterModel { Id = TestModel, Label = "GPT-4o Mini" }],
            SiteUrl = "http://localhost:5173",
            AppName = "AjuriIA Tests"
        };

        var httpClient = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(httpClient);

        return new OpenRouterService(
            factory,
            config,
            options,
            NullLogger<OpenRouterService>.Instance,
            TestModel);
    }

    private static HttpMessageHandler CreateSseHandler()
    {
        var content = string.Join("\n", new[]
        {
            "data: {\"choices\":[{\"delta\":{\"content\":\"Ol\"}}]}",
            "",
            "data: {\"choices\":[{\"delta\":{\"content\":\"á mundo\"}}]}",
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

    [Fact(DisplayName = "Given OpenRouter service, When created, Then Name is configured model")]
    public void Given_OpenRouterService_When_Created_Should_HaveConfiguredName()
    {
        // Given / When
        var sut = CreateSut(CreateSseHandler());

        // Then
        sut.Name.Should().Be(TestModel);
    }

    [Fact(DisplayName = "Given valid OpenRouter SSE response, When StreamAsync, Then yields streamed chunks")]
    public async Task Given_ValidSseResponse_When_StreamAsync_Should_YieldChunks()
    {
        // Given
        var sut = CreateSut(CreateSseHandler());
        var chunks = new List<string>();

        // When
        await foreach (var chunk in sut.StreamAsync("system", "user"))
            chunks.Add(chunk);

        // Then
        chunks.Should().Equal("Ol", "á mundo");
    }

    [Fact(DisplayName = "Given HTTP 401 response, When StreamAsync, Then throws HttpRequestException")]
    public async Task Given_Http401Response_When_StreamAsync_Should_ThrowHttpRequestException()
    {
        // Given
        var handler = new MockHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"unauthorized\"}", Encoding.UTF8, "application/json")
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
