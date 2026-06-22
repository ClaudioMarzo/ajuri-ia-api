using AjuriIA.API.Models;
using AjuriIA.API.Services;
using AjuriIA.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AjuriIA.Tests.Unit.Services;

public class LLMOrchestratorServiceTests
{
    private readonly Profile _profile = new()
    {
        Id = "professor",
        Llm = "claude-haiku",
        SystemPrompt = "Você é um assistente educacional."
    };

    private static async IAsyncEnumerable<string> YieldChunks(params string[] chunks)
    {
        foreach (var chunk in chunks) yield return chunk;
        await Task.CompletedTask;
    }

    [Fact(DisplayName = "Given primary LLM available, When StreamAsync, Then uses primary LLM")]
    public async Task Given_PrimaryLLMAvailable_When_StreamAsync_Should_UsePrimaryLLM()
    {
        // Given
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(YieldChunks("Olá"));
        var sut = new LLMOrchestratorService([claude], NullLogger<LLMOrchestratorService>.Instance);

        // When
        await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { }

        // Then
        sut.LastUsedLlm.Should().Be("claude-haiku");
    }

    [Fact(DisplayName = "Given primary LLM fails, When StreamAsync, Then uses first available fallback")]
    public async Task Given_PrimaryLLMFails_When_StreamAsync_Should_UseFirstAvailableFallback()
    {
        // Given
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws<HttpRequestException>();

        var openai = Substitute.For<ILLMService>();
        openai.Name.Returns("gpt-4o-mini");
        openai.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(YieldChunks("Resposta do OpenAI"));

        var sut = new LLMOrchestratorService([claude, openai], NullLogger<LLMOrchestratorService>.Instance);

        // When
        await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { }

        // Then
        sut.LastUsedLlm.Should().Be("gpt-4o-mini");
    }

    [Fact(DisplayName = "Given primary and fallback fail, When StreamAsync, Then uses last available LLM")]
    public async Task Given_PrimaryAndFallbackFail_When_StreamAsync_Should_UseLastAvailableLLM()
    {
        // Given
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws<HttpRequestException>();

        var openai = Substitute.For<ILLMService>();
        openai.Name.Returns("gpt-4o-mini");
        openai.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws<HttpRequestException>();

        var gemini = Substitute.For<ILLMService>();
        gemini.Name.Returns("gemini-flash");
        gemini.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(YieldChunks("Resposta do Gemini"));

        var sut = new LLMOrchestratorService([claude, openai, gemini], NullLogger<LLMOrchestratorService>.Instance);

        // When
        await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { }

        // Then
        sut.LastUsedLlm.Should().Be("gemini-flash");
    }

    [Fact(DisplayName = "Given all LLMs fail, When StreamAsync, Then throws AllLLMsUnavailableException")]
    public async Task Given_AllLLMsFail_When_StreamAsync_Should_ThrowAllLLMsUnavailableException()
    {
        // Given
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws<HttpRequestException>();

        var sut = new LLMOrchestratorService([claude], NullLogger<LLMOrchestratorService>.Instance);

        // When
        var act = async () =>
        {
            await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { }
        };

        // Then
        await act.Should().ThrowAsync<AllLLMsUnavailableException>();
    }

    [Fact(DisplayName = "Given primary LLM available, When StreamAsync, Then yields all chunks")]
    public async Task Given_PrimaryLLMAvailable_When_StreamAsync_Should_YieldAllChunks()
    {
        // Given
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(YieldChunks("Olá", " mundo", "!"));

        var sut = new LLMOrchestratorService([claude], NullLogger<LLMOrchestratorService>.Instance);
        var chunks = new List<string>();

        // When
        await foreach (var chunk in sut.StreamAsync(_profile, "mensagem", default))
            chunks.Add(chunk);

        // Then
        chunks.Should().HaveCount(3);
    }

    [Fact(DisplayName = "Given LLM responds, When StreamAsync, Then logs first chunk timing at Information")]
    public async Task Given_LLMResponds_When_StreamAsync_Then_LogsFirstChunkInfo()
    {
        // Given
        var logger = new CaptureLogger<LLMOrchestratorService>();
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(YieldChunks("Olá"));
        var sut = new LLMOrchestratorService([claude], logger);

        // When
        await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { }

        // Then
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("claude-haiku") &&
            e.Message.Contains("respondeu"));
    }

    [Fact(DisplayName = "Given LLM fails, When StreamAsync, Then logs warning with LLM name")]
    public async Task Given_LLMFails_When_StreamAsync_Then_LogsWarning()
    {
        // Given
        var logger = new CaptureLogger<LLMOrchestratorService>();
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws<HttpRequestException>();
        var gemini = Substitute.For<ILLMService>();
        gemini.Name.Returns("gemini-flash");
        gemini.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(YieldChunks("ok"));
        var sut = new LLMOrchestratorService([claude, gemini], logger);

        // When
        await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { }

        // Then
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning &&
            e.Message.Contains("claude-haiku"));
    }

    [Fact(DisplayName = "Given all LLMs fail, When StreamAsync, Then logs error with all LLM names")]
    public async Task Given_AllLLMsFail_When_StreamAsync_Then_LogsError()
    {
        // Given
        var logger = new CaptureLogger<LLMOrchestratorService>();
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws<HttpRequestException>();
        var sut = new LLMOrchestratorService([claude], logger);

        // When
        try { await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { } }
        catch (AllLLMsUnavailableException) { /* esperado — coberto por outro teste */ }

        // Then
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error &&
            e.Message.Contains("claude-haiku"));
    }

    [Fact(DisplayName = "Given primary fails and fallback used, When StreamAsync, Then logs fallback attempt")]
    public async Task Given_PrimaryFails_When_StreamAsync_Then_LogsFallbackAttempt()
    {
        // Given
        var logger = new CaptureLogger<LLMOrchestratorService>();
        var claude = Substitute.For<ILLMService>();
        claude.Name.Returns("claude-haiku");
        claude.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Throws<HttpRequestException>();
        var gemini = Substitute.For<ILLMService>();
        gemini.Name.Returns("gemini-flash");
        gemini.StreamAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(YieldChunks("ok"));
        var sut = new LLMOrchestratorService([claude, gemini], logger);

        // When
        await foreach (var _ in sut.StreamAsync(_profile, "mensagem", default)) { }

        // Then
        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Information &&
            e.Message.Contains("Fallback") &&
            e.Message.Contains("gemini-flash"));
    }
}
