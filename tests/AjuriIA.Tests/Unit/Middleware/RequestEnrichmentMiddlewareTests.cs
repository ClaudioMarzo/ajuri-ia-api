using AjuriIA.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace AjuriIA.Tests.Unit.Middleware;

public class RequestEnrichmentMiddlewareTests
{
    private static HttpContext CreateContext(string? jsonBody = null)
    {
        var context = new DefaultHttpContext();
        if (jsonBody is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentType = "application/json";
        }
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact(DisplayName = "Given body with profileId, When InvokeAsync, Then body is rewound for next middleware")]
    public async Task Given_BodyWithProfileId_When_InvokeAsync_Then_BodyIsRewound()
    {
        // Given
        var context = CreateContext("""{"profileId":"professor","message":"test"}""");
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new RequestEnrichmentMiddleware(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact(DisplayName = "Given body with profileId, When InvokeAsync, Then next is called")]
    public async Task Given_BodyWithProfileId_When_InvokeAsync_Then_NextIsCalled()
    {
        // Given
        var context = CreateContext("""{"profileId":"professor","message":"test"}""");
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new RequestEnrichmentMiddleware(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        nextCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "Given body without profileId, When InvokeAsync, Then next is still called")]
    public async Task Given_BodyWithoutProfileId_When_InvokeAsync_Then_NextIsCalled()
    {
        // Given
        var context = CreateContext("""{"message":"test"}""");
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new RequestEnrichmentMiddleware(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        nextCalled.Should().BeTrue();
    }

    [Fact(DisplayName = "Given non-JSON body, When InvokeAsync, Then does not throw")]
    public async Task Given_NonJsonBody_When_InvokeAsync_Then_DoesNotThrow()
    {
        // Given
        var context = CreateContext("not json at all");
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestEnrichmentMiddleware(next);

        // When
        var act = async () => await middleware.InvokeAsync(context);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "Given non-JSON body, When InvokeAsync, Then next is called")]
    public async Task Given_NonJsonBody_When_InvokeAsync_Then_NextIsCalled()
    {
        // Given
        var context = CreateContext("not json at all");
        var nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new RequestEnrichmentMiddleware(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        nextCalled.Should().BeTrue();
    }
}
