using System.Net;
using System.Text.Json;
using AjuriIA.API.Middleware;
using AjuriIA.API.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AjuriIA.Tests.Unit.Middleware;

public class ExceptionHandlerMiddlewareTests
{
    private static (ExceptionHandlerMiddleware middleware, HttpContext context) CreateSut(
        RequestDelegate next)
    {
        var logger = Substitute.For<ILogger<ExceptionHandlerMiddleware>>();
        var middleware = new ExceptionHandlerMiddleware(next, logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return (middleware, context);
    }

    [Fact(DisplayName = "Given unhandled exception, When request, Then returns HTTP 500")]
    public async Task Given_UnhandledException_When_Request_Should_ReturnHttp500()
    {
        // Given
        RequestDelegate next = _ => throw new Exception("erro inesperado");
        var (middleware, context) = CreateSut(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact(DisplayName = "Given AllLLMsUnavailableException, When request, Then returns HTTP 503")]
    public async Task Given_AllLLMsUnavailableException_When_Request_Should_ReturnHttp503()
    {
        // Given
        RequestDelegate next = _ => throw new AllLLMsUnavailableException("todos falharam");
        var (middleware, context) = CreateSut(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact(DisplayName = "Given unhandled exception, When request, Then response body contains success false")]
    public async Task Given_UnhandledException_When_Request_Should_ReturnSuccessFalse()
    {
        // Given
        RequestDelegate next = _ => throw new Exception("erro");
        var (middleware, context) = CreateSut(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<ApiResponse<object>>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response!.Success.Should().BeFalse();
    }

    [Fact(DisplayName = "Given unhandled exception, When request, Then response does not contain stack trace")]
    public async Task Given_UnhandledException_When_Request_Should_NotContainStackTrace()
    {
        // Given
        RequestDelegate next = _ => throw new Exception("erro interno");
        var (middleware, context) = CreateSut(next);

        // When
        await middleware.InvokeAsync(context);

        // Then
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().NotContain("StackTrace");
    }
}
